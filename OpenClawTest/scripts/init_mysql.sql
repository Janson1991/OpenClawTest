-- ===================================================================
-- skudetail2 全文索引 DDL
-- 基于用户实际表结构，添加 ngram 全文索引支持中文搜索
-- ===================================================================

-- 确认 MySQL 配置: ngram_token_size = 2
-- [mysqld]
-- ngram_token_size = 2

USE sku_search;

-- 为 skudetail2 添加全文索引（name + spu_item_name）
-- ⚠️ 如果表有几百万数据，建议在低峰期执行，或用 pt-online-schema-change
ALTER TABLE skudetail2
    ADD FULLTEXT INDEX ft_search (name, spu_item_name) WITH PARSER ngram;

-- 验证索引创建成功
SELECT * FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'sku_search'
  AND TABLE_NAME = 'skudetail2'
  AND INDEX_TYPE = 'FULLTEXT';

-- ===================================================================
-- 测试查询
-- ===================================================================

-- 1. 全文搜索："茶具"
SELECT record_id, goods_id, name, brand_name, price_sale,
       MATCH(name, spu_item_name) AGAINST('+茶具*' IN BOOLEAN MODE) AS score
FROM skudetail2
WHERE MATCH(name, spu_item_name) AGAINST('+茶具*' IN BOOLEAN MODE)
  AND deleted = 0 AND state = 1
ORDER BY score DESC
LIMIT 10;

-- 2. 全文搜索："羽毛球"
SELECT record_id, goods_id, name, brand_name, price_sale,
       MATCH(name, spu_item_name) AGAINST('+羽毛球*' IN BOOLEAN MODE) AS score
FROM skudetail2
WHERE MATCH(name, spu_item_name) AGAINST('+羽毛球*' IN BOOLEAN MODE)
  AND deleted = 0 AND state = 1
ORDER BY score DESC
LIMIT 10;

-- 3. 按店铺搜索
SELECT record_id, goods_id, name, price_sale,
       MATCH(name, spu_item_name) AGAINST('+紫砂*' IN BOOLEAN MODE) AS score
FROM skudetail2
WHERE MATCH(name, spu_item_name) AGAINST('+紫砂*' IN BOOLEAN MODE)
  AND shop_id = 1100
  AND deleted = 0 AND state = 1
ORDER BY score DESC
LIMIT 10;
