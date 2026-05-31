# 项目架构与流程图

> 所有图表使用 Mermaid 语法，GitHub / VSCode 原生渲染

---

## 1. 系统架构图

```mermaid
graph TB
    subgraph Frontend["🖥️ Vue 3 前端"]
        UI[SearchView.vue]
        SB[SearchBar.vue<br/>文字 + 语音输入]
        SC[SkuCard.vue<br/>商品卡片]
        Store[Pinia Store]
    end

    subgraph API["🌐 .NET 8 Web API"]
        SC1[SearchController<br/>POST /api/search]
        SC2[AdminController<br/>POST /api/admin/index/rebuild]
        SearchSvc[SearchService<br/>搜索编排 + RRF融合]
        AISvc[AiQueryParser<br/>AI 意图解析]
        EmbedSvc[IEmbeddingService<br/>向量化]
        IndexSvc[IndexingService<br/>批量建索引]
    end

    subgraph AI["🧠 AI 服务"]
        OpenAI[OpenAI API<br/>gpt-4o-mini + embedding]
        Ollama[Ollama 本地<br/>bge-m3 + qwen2.5]
    end

    subgraph Storage["💾 存储层"]
        MySQL[(MySQL<br/>skus 表<br/>百万级商品)]
        Qdrant[(Qdrant<br/>sku_vectors<br/>向量索引)]
        Redis[(Redis<br/>搜索缓存<br/>5min TTL)]
    end

    UI --> SB
    SB --> Store
    Store --> SC1
    SC1 --> SearchSvc
    SC1 --> AISvc
    SC1 --> EmbedSvc
    SC1 --> Redis
    SearchSvc --> Qdrant
    SearchSvc --> MySQL
    SC2 --> IndexSvc
    IndexSvc --> EmbedSvc
    IndexSvc --> MySQL
    IndexSvc --> Qdrant
    AISvc --> OpenAI
    AISvc --> Ollama
    EmbedSvc --> OpenAI
    EmbedSvc --> Ollama
```

---

## 2. 搜索流程图

```mermaid
sequenceDiagram
    actor User as 👤 用户
    participant FE as 🖥️ Vue 前端
    participant API as 🌐 .NET API
    participant AI as 🧠 AI 解析
    participant Cache as ⚡ Redis 缓存
    participant Vec as 🔵 Qdrant 向量库
    participant DB as 🟡 MySQL 全文索引

    User->>FE: 输入"户外用品" / 语音识别
    FE->>API: POST /api/search {query:"户外用品"}
    
    API->>Cache: 查询缓存 key=search:户外用品
    
    alt 缓存命中
        Cache-->>API: 返回缓存结果
        API-->>FE: 200 OK (缓存)
    else 缓存未命中
        API->>AI: 解析意图
        AI-->>API: {keywords:["户外"], synonyms:["帐篷","睡袋","露营椅","防潮垫"...]}
        
        par 并行搜索
            API->>Vec: 向量搜索 (embedding + 余弦相似度)
            Vec-->>API: Top K 语义结果
        and
            API->>DB: FULLTEXT SEARCH (ngram 分词)
            DB-->>API: Top K 关键词结果
        end
        
        API->>API: RRF 融合排序
        API->>Cache: 写入缓存 (TTL=5min)
        API-->>FE: 200 OK (合并结果)
    end
    
    FE-->>User: 渲染商品网格 + AI理解标签
```

---

## 3. 全量建索引流程图

```mermaid
flowchart TD
    Start([触发 POST /api/admin/index/rebuild]) --> CreateCol{Qdrant 集合<br/>是否存在?}
    
    CreateCol -->|不存在| CreateNew[创建 sku_vectors 集合<br/>维度=1536 距离=Cosine<br/>OnDisk=true]
    CreateCol -->|已存在| SkipCreate[跳过，增量模式]
    
    CreateNew --> CountDB[查询 MySQL 总商品数]
    SkipCreate --> CountDB
    
    CountDB --> Loop{分批读取<br/>每批 200 条}
    
    Loop -->|有数据| BuildText[拼接搜索文本<br/>name + category + brand + tags + desc]
    BuildText --> EmbedBatch[批量调用 Embedding API<br/>生成向量 1536维]
    EmbedBatch --> BuildPoints[构造 PointStruct<br/>Payload 含冗余字段]
    BuildPoints --> Upsert[Qdrant Upsert 写入]
    Upsert --> Progress[输出进度日志<br/>已处理/总数 百分比 预计剩余]
    Progress --> Loop
    
    Loop -->|无数据| Done([✅ 索引构建完成])
    Loop -->|异常| Retry[等待 2 秒<br/>跳过本批继续]
    Retry --> Loop
    
    style Start fill:#4CAF50,color:#fff
    style Done fill:#4CAF50,color:#fff
    style EmbedBatch fill:#FF9800,color:#fff
    style Upsert fill:#2196F3,color:#fff
```

---

## 4. 增量同步流程图

```mermaid
sequenceDiagram
    participant Biz as 📦 业务代码
    participant Svc as 💾 SkuService
    participant DB as 🟡 MySQL
    participant Idx as 🔄 IndexingService
    participant Emb as 🧠 Embedding
    participant Vec as 🔵 Qdrant

    Note over Biz,Vec: 商品新增/更新场景
    Biz->>Svc: SaveAsync(sku)
    Svc->>DB: INSERT / UPDATE skus
    Svc->>DB: SaveChanges
    Svc->>Idx: UpsertSkuAsync(sku) [异步]
    Svc-->>Biz: 返回 sku
    
    Idx->>Idx: BuildSearchText(sku)
    Idx->>Emb: GetEmbeddingAsync(text)
    Emb-->>Idx: float[1536]
    Idx->>Vec: Upsert Point {id, vector, payload}

    Note over Biz,Vec: 商品删除场景
    Biz->>Svc: DeleteAsync(id)
    Svc->>DB: DELETE skus WHERE id=?
    Svc->>DB: SaveChanges
    Svc->>Idx: DeleteSkuAsync(id) [异步]
    Idx->>Vec: Delete Point {id}
```

---

## 5. 数据库表结构

```mermaid
erDiagram
    SKUS {
        bigint id PK "主键"
        varchar sku_code UK "商品编码"
        varchar name "商品名称"
        text description "商品描述"
        varchar category "分类"
        varchar tags "标签(逗号分隔)"
        varchar brand "品牌"
        decimal price "价格"
        varchar image_url "图片地址"
        tinyint is_active "是否启用"
        datetime created_at
        datetime updated_at
    }

    SKUS ||--o{ SKU_VECTORS : "向量索引"
    SKUS ||--o{ SKU_CACHE : "搜索缓存"
```

---

## 6. 向量搜索 vs 全文搜索对比

```mermaid
graph LR
    subgraph Query["用户输入: 户外用品"]
        Q[查询文本]
    end

    subgraph VectorPath["🔵 向量搜索路径"]
        E[Embedding 编码<br/>1536维向量]
        V[Qdrant Cosine 搜索<br/>语义相似度]
    end

    subgraph KeywordPath["🟡 全文搜索路径"]
        AI2[AI 解析扩展]
        N[MySQL ngram 分词<br/>关键词匹配]
    end

    subgraph RRF["🔀 RRF 融合"]
        R[倒数排名融合<br/>k=60]
    end

    subgraph Result["📊 最终结果"]
        TOP[Top K 商品<br/>相关性排序]
    end

    Q --> E --> V --> R
    Q --> AI2 --> N --> R
    R --> TOP
```

---

## 7. 部署架构

```mermaid
graph TB
    subgraph Local["💻 开发环境"]
        FE["Vue 前端<br/>localhost:5173"]
        API[".NET API<br/>localhost:5000"]
        MySQL["MySQL<br/>localhost:3306"]
        Qdrant["Qdrant<br/>localhost:6333/6334"]
        Redis["Redis<br/>localhost:6379"]
        Ollama["Ollama<br/>localhost:11434<br/>(可选)"]
    end

    subgraph Docker["🐳 Docker Compose"]
        DQ["Qdrant 容器"]
        DR["Redis 容器"]
    end

    subgraph Cloud["☁️ AI 云服务"]
        OpenAI["OpenAI API<br/>gpt-4o-mini<br/>text-embedding-3-small"]
    end

    FE -.->|Vite Proxy /api| API
    API --> MySQL
    API --> Qdrant
    API --> Redis
    API -.->|EmbeddingProvider=openai| OpenAI
    API -.->|EmbeddingProvider=ollama| Ollama

    Docker -.->|端口映射| Qdrant
    Docker -.->|端口映射| Redis
```

---

## 8. 技术栈总览

```mermaid
mindmap
  root((SKU 智能搜索))
    前端
      Vue 3
      Pinia 状态管理
      Element Plus UI
      Axios HTTP
      Vite 构建
      Web Speech API 语音
    后端
      .NET 8 Web API
      Clean Architecture
      Serilog 日志
      Swagger 文档
      Redis 缓存
    AI
      OpenAI / DeepSeek / 通义千问
      Ollama 本地离线
      gpt-4o-mini 意图解析
      text-embedding-3-small 向量化
    搜索
      Qdrant 向量数据库
      MySQL FULLTEXT ngram
      RRF 融合排序
      语义 + 关键词双路
    基础设施
      Docker Compose
      MySQL 8
      Redis 7
      Qdrant 最新版
```
