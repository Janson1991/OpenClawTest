-- 插入示例数据用于测试
USE product_selector;

-- 示例商品数据
INSERT INTO Products (Name, ImageUrl, Price, OriginalPrice, SalesCount, ReviewCount, Rating, Platform, Category, SourceUrl, ShopName, Location) VALUES
('无线蓝牙耳机 降噪运动', 'https://via.placeholder.com/100', 89.90, 199.00, 15680, 3240, 4.80, '1688', '电子产品', 'https://detail.1688.com/offer/123.html', '深圳数码科技', '广东深圳'),
('智能手表 运动健康监测', 'https://via.placeholder.com/100', 159.00, 299.00, 8920, 1856, 4.65, '1688', '电子产品', 'https://detail.1688.com/offer/456.html', '东莞智能设备', '广东东莞'),
('便携式充电宝 20000mAh', 'https://via.placeholder.com/100', 45.50, 89.00, 23450, 5670, 4.90, '1688', '电子产品', 'https://detail.1688.com/offer/789.html', '深圳电源科技', '广东深圳'),
('夏季清凉防晒衣 女士', 'https://via.placeholder.com/100', 35.00, 69.00, 45600, 8920, 4.75, '1688', '服装', 'https://detail.1688.com/offer/101.html', '杭州服饰有限公司', '浙江杭州'),
('多功能厨房收纳架', 'https://via.placeholder.com/100', 28.80, 58.00, 12340, 2340, 4.85, '1688', '家居', 'https://detail.1688.com/offer/202.html', '义乌家居用品', '浙江义乌'),
('儿童益智积木玩具', 'https://via.placeholder.com/100', 42.00, 88.00, 34560, 6780, 4.70, '1688', '玩具', 'https://detail.1688.com/offer/303.html', '汕头玩具厂', '广东汕头'),
('男士商务双肩包 防水', 'https://via.placeholder.com/100', 68.00, 138.00, 9870, 1980, 4.60, '1688', '箱包', 'https://detail.1688.com/offer/404.html', '广州箱包厂', '广东广州'),
('宠物自动喂食器 智能', 'https://via.placeholder.com/100', 128.00, 258.00, 6540, 1230, 4.55, '1688', '宠物用品', 'https://detail.1688.com/offer/505.html', '深圳宠物科技', '广东深圳'),
('LED化妆镜 台式', 'https://via.placeholder.com/100', 56.00, 118.00, 18760, 3450, 4.80, '1688', '美妆工具', 'https://detail.1688.com/offer/606.html', '广州美妆工具', '广东广州'),
('迷你投影仪 家用高清', 'https://via.placeholder.com/100', 299.00, 599.00, 4320, 890, 4.50, '1688', '电子产品', 'https://detail.1688.com/offer/707.html', '深圳投影设备', '广东深圳');

-- 示例价格历史
INSERT INTO PriceHistory (ProductId, Price, RecordedAt) VALUES
(1, 99.90, DATE_SUB(NOW(), INTERVAL 7 DAY)),
(1, 95.00, DATE_SUB(NOW(), INTERVAL 5 DAY)),
(1, 89.90, DATE_SUB(NOW(), INTERVAL 2 DAY)),
(2, 179.00, DATE_SUB(NOW(), INTERVAL 7 DAY)),
(2, 169.00, DATE_SUB(NOW(), INTERVAL 3 DAY)),
(2, 159.00, DATE_SUB(NOW(), INTERVAL 1 DAY)),
(3, 55.00, DATE_SUB(NOW(), INTERVAL 6 DAY)),
(3, 49.90, DATE_SUB(NOW(), INTERVAL 3 DAY)),
(3, 45.50, DATE_SUB(NOW(), INTERVAL 1 DAY));

-- 示例标签
INSERT INTO ProductTags (Name, ProductId) VALUES
('热销', 1), ('新款', 1), ('电子产品', 1),
('热销', 2), ('智能', 2), ('电子产品', 2),
('爆款', 3), ('高销量', 3), ('电子产品', 3),
('夏季', 4), ('女士', 4), ('防晒', 4),
('实用', 5), ('收纳', 5), ('家居', 5),
('儿童', 6), ('益智', 6), ('玩具', 6),
('商务', 7), ('防水', 7), ('箱包', 7),
('宠物', 8), ('智能', 8), ('宠物用品', 8),
('美妆', 9), ('LED', 9), ('美妆工具', 9),
('高清', 10), ('投影', 10), ('电子产品', 10);
