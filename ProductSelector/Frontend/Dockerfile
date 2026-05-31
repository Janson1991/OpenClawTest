# 前端Dockerfile
FROM node:18-alpine AS build
WORKDIR /app

# 复制package.json并安装依赖
COPY package*.json ./
RUN npm install

# 复制所有文件并构建
COPY . .
RUN npm run build

# 生产环境
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# 复制构建结果
COPY --from=build /app/dist .

# 复制nginx配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
