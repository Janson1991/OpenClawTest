# ProductSelector - 选品监控工具

## 功能特性

### 核心功能
- 🔍 **商品抓取** - 支持1688平台商品数据抓取
- 📊 **数据展示** - 商品列表、搜索、筛选
- 🤖 **AI分析** - 智能商品分析、推荐、对比
- 📈 **价格监控** - 价格变动提醒、历史记录
- 📊 **数据仪表盘** - 可视化数据展示
- 📥 **数据导出** - CSV格式导出

### 用户功能
- 👤 **用户认证** - JWT登录注册
- ❤️ **商品收藏** - 收藏夹管理
- 📝 **搜索历史** - 记录搜索关键词
- 🔔 **价格提醒** - 设置目标价格提醒

### 技术特性
- 🚀 **高性能** - .NET 8 + Vue 3
- 🐳 **容器化** - Docker一键部署
- 🔐 **安全认证** - JWT Token认证
- 📱 **响应式** - 适配多种设备

## 技术栈

### 后端
- **框架**: .NET 8 ASP.NET Core Web API
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core
- **认证**: JWT Bearer
- **爬虫**: HtmlAgilityPack

### 前端
- **框架**: Vue 3
- **UI库**: Element Plus
- **构建工具**: Vite
- **HTTP客户端**: Axios

## 快速开始

### 方式一：Docker部署（推荐）

```bash
# 克隆项目
git clone <repository-url>
cd ProductSelector

# 启动所有服务
docker-compose up -d

# 访问应用
# 前端: http://localhost
# 后端API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 方式二：本地开发

#### 1. 启动MySQL
```bash
# 使用Docker启动MySQL
docker run -d \
  --name mysql \
  -e MYSQL_ROOT_PASSWORD=123456 \
  -e MYSQL_DATABASE=product_selector \
  -p 3306:3306 \
  mysql:8.0

# 初始化数据库
mysql -u root -p123456 product_selector < Database/init.sql
mysql -u root -p123456 product_selector < Database/sample_data.sql
```

#### 2. 启动后端
```bash
cd Backend

# 还原依赖
dotnet restore

# 运行
dotnet run
```

#### 3. 启动前端
```bash
cd Frontend

# 安装依赖
npm install

# 启动开发服务器
npm run dev
```

## 项目结构

```
ProductSelector/
├── Backend/                    # 后端API
│   ├── Controllers/           # API控制器
│   ├── Services/              # 业务逻辑
│   ├── Models/                # 数据模型
│   ├── Data/                  # 数据库上下文
│   ├── Properties/            # 配置文件
│   ├── Program.cs             # 入口文件
│   ├── ProductSelector.csproj # 项目文件
│   └── Dockerfile             # Docker配置
├── Frontend/                   # 前端应用
│   ├── src/                   # 源代码
│   │   ├── views/             # 页面组件
│   │   ├── components/        # 公共组件
│   │   └── api/               # API接口
│   ├── public/                # 静态资源
│   ├── index.html             # 主页面
│   ├── ai-analysis.html       # AI分析页面
│   ├── dashboard.html         # 仪表盘页面
│   ├── package.json           # 依赖配置
│   ├── vite.config.js         # Vite配置
│   ├── nginx.conf             # Nginx配置
│   └── Dockerfile             # Docker配置
├── Database/                   # 数据库脚本
│   ├── init.sql               # 初始化脚本
│   └── sample_data.sql        # 示例数据
├── docker-compose.yml         # Docker Compose配置
└── README.md                  # 项目说明
```

## API接口

### 认证接口
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/auth/profile` - 获取用户信息
- `PUT /api/auth/profile` - 更新用户信息

### 商品接口
- `GET /api/products` - 获取商品列表
- `GET /api/products/{id}` - 获取商品详情
- `POST /api/products` - 创建商品
- `PUT /api/products/{id}` - 更新商品
- `DELETE /api/products/{id}` - 删除商品
- `GET /api/products/search` - 搜索商品
- `GET /api/products/trending` - 获取热门商品
- `POST /api/products/scrape` - 抓取商品数据

### AI分析接口
- `POST /api/aianalysis/analyze/{id}` - 分析商品
- `POST /api/aianalysis/batch-analyze` - 批量分析
- `GET /api/aianalysis/market-report` - 市场报告
- `GET /api/aianalysis/recommendations` - 获取推荐
- `POST /api/aianalysis/compare` - 对比商品

### 价格监控接口
- `GET /api/pricemonitor/alerts/{userId}` - 获取监控提醒
- `POST /api/pricemonitor/alerts` - 创建监控提醒
- `DELETE /api/pricemonitor/alerts/{id}` - 删除监控提醒
- `POST /api/pricemonitor/check-alerts` - 检查提醒
- `GET /api/pricemonitor/history/{productId}` - 价格历史

### 收藏接口
- `GET /api/favorites` - 获取收藏列表
- `POST /api/favorites/add/{productId}` - 添加收藏
- `DELETE /api/favorites/remove/{productId}` - 移除收藏
- `GET /api/favorites/check/{productId}` - 检查收藏状态

### 搜索历史接口
- `GET /api/searchhistory` - 获取搜索历史
- `POST /api/searchhistory/record` - 记录搜索
- `DELETE /api/searchhistory/clear` - 清空历史
- `GET /api/searchhistory/popular` - 热门关键词

## 配置说明

### 数据库配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=product_selector;User=root;Password=123456;"
  }
}
```

### JWT配置
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long"
  }
}
```

## 开发说明

### 添加新功能
1. 在 `Backend/Models/` 添加数据模型
2. 在 `Backend/Services/` 添加业务逻辑
3. 在 `Backend/Controllers/` 添加API接口
4. 在 `Frontend/src/views/` 添加前端页面
5. 更新 `AppDbContext` 添加新的DbSet

### 代码规范
- 遵循C#编码规范
- 使用依赖注入
- 异步编程优先
- 统一异常处理

## 部署说明

### 生产环境
1. 修改 `appsettings.json` 中的数据库连接字符串
2. 修改JWT密钥
3. 设置环境变量 `ASPNETCORE_ENVIRONMENT=Production`
4. 使用Docker Compose部署

### 监控
- 健康检查: `GET /health`
- Swagger文档: `GET /swagger`

## 许可证

MIT License
