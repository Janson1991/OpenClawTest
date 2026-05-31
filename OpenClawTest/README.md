# SKU 智能搜索

基于 **.NET 8 Web API + Vue 3** 构建的商品智能搜索工具，支持：
- 🧠 **AI 语义解析**：理解"户外用品"→自动扩展帐篷、睡袋、露营椅等
- 🔍 **向量语义搜索**：Qdrant 本地向量库，毫秒级响应
- 📝 **全文关键词搜索**：MySQL ngram 中文分词，精确匹配
- 🔀 **RRF 融合排序**：两路结果智能合并，相关性最优
- 🎤 **语音输入**：浏览器 Web Speech API，免费无需额外服务
- ⚡ **Redis 缓存**：高频搜索缓存5分钟，降低 AI API 费用

---

## 项目结构

```
OpenClawTest/
├── src/
│   ├── Api/                    # .NET 8 Web API 入口
│   │   ├── Controllers/
│   │   │   ├── SearchController.cs   # 搜索接口
│   │   │   └── AdminController.cs    # 建索引管理
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── Application/            # 业务逻辑层
│   │   ├── Services/
│   │   │   ├── IServices.cs          # 接口定义
│   │   │   └── SearchService.cs      # 搜索编排 + RRF 融合
│   │   └── DTOs/
│   │       └── SearchDtos.cs
│   ├── Domain/                 # 领域实体
│   │   └── Entities/Sku.cs
│   └── Infrastructure/         # 基础设施实现
│       ├── Data/AppDbContext.cs
│       ├── Repositories/
│       │   └── SearchRepositories.cs  # Qdrant + MySQL 搜索
│       └── Services/
│           ├── AiQueryParser.cs       # AI 意图解析
│           ├── EmbeddingService.cs    # OpenAI / Ollama Embedding
│           └── IndexingService.cs     # 批量建向量索引
├── frontend/sku-search-vue/    # Vue 3 前端
│   └── src/
│       ├── api/search.ts
│       ├── stores/search.ts
│       ├── components/
│       │   ├── SearchBar.vue         # 搜索框 + 语音
│       │   └── SkuCard.vue           # 商品卡片
│       └── views/SearchView.vue
├── scripts/init_mysql.sql      # 建表 DDL
└── docker-compose.yml          # Qdrant + Redis
```

---

## 快速启动

### 1. 启动 Qdrant + Redis

```bash
cd OpenClawTest
docker-compose up -d

# 验证 Qdrant
curl http://localhost:6333/healthz

# Qdrant Web UI
open http://localhost:6333/dashboard
```

### 2. 初始化 MySQL

```bash
# 修改 my.cnf，添加中文分词配置
# [mysqld]
# ngram_token_size = 2

mysql -u root -p < scripts/init_mysql.sql
```

### 3. 配置 appsettings.json

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Database=sku_search;User=root;Password=你的密码;",
    "Redis": "localhost:6379"
  },
  "AI": {
    "EmbeddingProvider": "openai",
    "ApiKey": "sk-你的key",
    "Model": "gpt-4o-mini",
    "EmbeddingModel": "text-embedding-3-small"
  }
}
```

> 💡 **完全离线方案**：将 `EmbeddingProvider` 改为 `ollama`，并运行：
> ```bash
> ollama pull bge-m3 && ollama serve
> ```

### 4. 启动 .NET API

```bash
cd src/Api
dotnet run
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 5. 构建向量索引（首次必须）

```bash
curl -X POST http://localhost:5000/api/admin/index/rebuild

# 查看进度（观察日志或）
curl http://localhost:5000/api/admin/index/status
```

### 6. 启动 Vue 前端

```bash
cd frontend/sku-search-vue
npm install
npm run dev
# 浏览器: http://localhost:5173
```

---

## API 文档

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/search` | 智能搜索（推荐） |
| GET  | `/api/search?q=户外用品` | 快速搜索 |
| POST | `/api/admin/index/rebuild` | 触发全量建索引 |
| GET  | `/api/admin/index/status` | 查询索引状态 |
| POST | `/api/admin/index/cancel` | 取消建索引 |

**搜索请求体示例：**
```json
{
  "query": "户外用品",
  "topK": 20,
  "category": null,
  "minScore": 0.55
}
```

---

## 向量维度说明

| Embedding 模型 | 维度 | 修改位置 |
|---|---|---|
| `text-embedding-3-small`（OpenAI） | **1536** | `IndexingService.VectorSize` |
| `bge-m3`（Ollama，推荐中文） | **1024** | 同上，改为 1024 |
| `nomic-embed-text`（Ollama，轻量） | **768** | 同上，改为 768 |

---

## 百万商品性能参考

| 阶段 | 耗时估算 |
|------|------|
| 全量建索引（OpenAI） | ~1.5 小时 |
| 全量建索引（Ollama 本地） | ~4~6 小时 |
| 单次搜索响应（缓存命中） | < 5ms |
| 单次搜索响应（无缓存） | 100~500ms |
