-- 选品监控工具数据库初始化脚本
-- ProductSelector Database Schema

CREATE DATABASE IF NOT EXISTS product_selector;
USE product_selector;

-- 商品表
CREATE TABLE IF NOT EXISTS Products (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(500) NOT NULL,
    ImageUrl VARCHAR(1000) NULL,
    Price DECIMAL(18, 2) NOT NULL DEFAULT 0,
    OriginalPrice DECIMAL(18, 2) NULL,
    SalesCount INT NOT NULL DEFAULT 0,
    ReviewCount INT NOT NULL DEFAULT 0,
    Rating DECIMAL(3, 2) NOT NULL DEFAULT 0,
    Platform VARCHAR(50) NULL,
    Category VARCHAR(100) NULL,
    SourceUrl VARCHAR(1000) NULL,
    ShopName VARCHAR(200) NULL,
    Location VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_platform (Platform),
    INDEX idx_category (Category),
    INDEX idx_sales_count (SalesCount),
    INDEX idx_created_at (CreatedAt),
    INDEX idx_price (Price)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 商品标签表
CREATE TABLE IF NOT EXISTS ProductTags (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    ProductId INT NOT NULL,
    
    INDEX idx_product_id (ProductId),
    INDEX idx_tag_name (Name),
    
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 监控记录表（用于记录价格变动等）
CREATE TABLE IF NOT EXISTS PriceHistory (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProductId INT NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    RecordedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_product_id (ProductId),
    INDEX idx_recorded_at (RecordedAt),
    
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 用户监控列表
CREATE TABLE IF NOT EXISTS UserMonitors (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProductId INT NOT NULL,
    UserId VARCHAR(100) NOT NULL,
    AlertOnPriceDrop BOOLEAN NOT NULL DEFAULT FALSE,
    AlertOnNewReview BOOLEAN NOT NULL DEFAULT FALSE,
    TargetPrice DECIMAL(18, 2) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_user_id (UserId),
    INDEX idx_product_id (ProductId),
    UNIQUE KEY uk_user_product (UserId, ProductId),
    
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
