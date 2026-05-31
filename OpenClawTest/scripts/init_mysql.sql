-- ===================================================================
-- skudetail2 全文索引 DDL
-- 基于用户实际表结构，添加 ngram 全文索引支持中文搜索
-- ===================================================================

-- 确认 MySQL 配置: ngram_token_size = 2 (支持中文2字分词)
-- [mysqld]
-- ngram_token_size = 2

USE sku_search;

-- 为 skudetail2 添加全文索引（name + spu_item_name）
-- 注意：如果表数据量很大，ALTER TABLE 可能需要几分钟
ALTER TABLE skudetail2
    ADD FULLTEXT INDEX ft_search (name, spu_item_name) WITH PARSER ngram;

-- 验证索引
SHOW INDEX FROM skudetail2 WHERE Key_name = 'ft_search';

-- ===================================================================
-- 测试查询
-- ===================================================================

-- 1. 全文搜索测试
SELECT record_id, goods_id, name, spu_item_name, brand_name, price_sale,
       MATCH(name, spu_item_name) AGAINST('+户外*' IN BOOLEAN MODE) AS score
FROM skudetail2
WHERE MATCH(name, spu_item_name) AGAINST('+户外*' IN BOOLEAN MODE)
  AND deleted = 0
  AND state = 1
  AND auto_state = 1
ORDER BY score DESC
LIMIT 10;

-- 2. 按店铺搜索
SELECT record_id, goods_id, name, spu_item_name, brand_name, price_sale,
       MATCH(name, spu_item_name) AGAINST('+帐篷*' IN BOOLEAN MODE) AS score
FROM skudetail2
WHERE MATCH(name, spu_item_name) AGAINST('+帐篷*' IN BOOLEAN MODE)
  AND shop_id = 1
  AND deleted = 0
  AND state = 1
  AND auto_state = 1
ORDER BY score DESC
LIMIT 10;

-- 3. 查看全文索引状态
SELECT * FROM information_schema.STATISTICS
WHERE TABLE_NAME = 'skudetail2' AND INDEX_TYPE = 'FULLTEXT';
