-- MySQL 建表 DDL
-- 运行前确认 MySQL 配置: ngram_token_size = 2 (支持中文2字分词)

CREATE DATABASE IF NOT EXISTS sku_search
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE sku_search;

CREATE TABLE IF NOT EXISTS skus (
    id          BIGINT          NOT NULL AUTO_INCREMENT,
    sku_code    VARCHAR(64)     NOT NULL,
    name        VARCHAR(255)    NOT NULL,
    description TEXT,
    category    VARCHAR(128),
    tags        VARCHAR(512)    COMMENT '逗号分隔标签: 帐篷,户外,露营',
    brand       VARCHAR(128),
    price       DECIMAL(10, 2)  NOT NULL DEFAULT 0.00,
    image_url   VARCHAR(512),
    is_active   TINYINT(1)      NOT NULL DEFAULT 1,
    created_at  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uk_sku_code (sku_code),
    INDEX       idx_category  (category),
    INDEX       idx_is_active (is_active),
    INDEX       idx_updated   (updated_at),

    -- 全文索引：支持中文 ngram 分词
    FULLTEXT INDEX ft_search (name, description, tags) WITH PARSER ngram
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- MySQL 配置建议 (my.cnf / my.ini)
-- [mysqld]
-- ngram_token_size = 2
-- ft_min_word_len   = 2
-- innodb_ft_min_token_size = 2


-- 测试数据（模拟户外商品）
INSERT INTO skus (sku_code, name, category, tags, brand, price, is_active) VALUES
('OUT001', '三人自动帐篷 防雨防风', '户外装备', '帐篷,露营,防雨,三季,户外', '牧高笛', 599.00, 1),
('OUT002', '折叠露营椅 铝合金轻便', '户外装备', '露营椅,折叠椅,户外椅,轻量', '探路者', 129.00, 1),
('OUT003', '防潮垫 加厚铝膜地垫', '户外装备', '防潮垫,地垫,露营,防潮', '凯乐石', 89.00, 1),
('OUT004', '登山杖 碳纤维超轻折叠', '户外装备', '登山杖,碳纤维,折叠,轻量', 'Leki', 399.00, 1),
('OUT005', '头灯 强光充电 防水', '户外装备', '头灯,强光,充电,防水,露营', '黑钻', 199.00, 1),
('OUT006', '睡袋 零下10度 木乃伊型', '户外装备', '睡袋,冬季,零下10度,木乃伊', '骆驼', 329.00, 1),
('OUT007', '户外炊具套装 铝合金锅', '户外装备', '炊具,套装,铝合金,野炊,露营', '柯曼', 259.00, 1),
('OUT008', '登山包 50L 防水双肩包', '户外装备', '登山包,背包,50L,防水,双肩', '迪卡侬', 449.00, 1);
