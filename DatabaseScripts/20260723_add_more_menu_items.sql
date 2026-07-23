-- Add more menu items for a richer sushi restaurant menu.
-- Safe to run multiple times:
-- - Existing categories are restored if soft-deleted.
-- - Existing items with the same item_name are restored and updated.
-- - Category names are standardized to English.

USE RestaurantSystemDB;

INSERT INTO menu_categories (category_name, deleted_at)
VALUES
    ('Sushi & Sashimi', NULL),
    ('Hot Kitchen', NULL),
    ('Rice & Noodles', NULL),
    ('Beverages', NULL),
    ('Desserts', NULL)
ON DUPLICATE KEY UPDATE
    deleted_at = NULL;

UPDATE menu_items mi
JOIN menu_categories old_category ON old_category.category_id = mi.category_id
JOIN menu_categories new_category ON new_category.category_name = 'Hot Kitchen'
SET mi.category_id = new_category.category_id
WHERE old_category.category_name = 'Mon nong'
   OR old_category.category_name = 'Món nóng';

UPDATE menu_items mi
JOIN menu_categories old_category ON old_category.category_id = mi.category_id
JOIN menu_categories new_category ON new_category.category_name = 'Rice & Noodles'
SET mi.category_id = new_category.category_id
WHERE old_category.category_name = 'Mi & Com'
   OR old_category.category_name = 'Mì & Cơm';

UPDATE menu_items mi
JOIN menu_categories old_category ON old_category.category_id = mi.category_id
JOIN menu_categories new_category ON new_category.category_name = 'Beverages'
SET mi.category_id = new_category.category_id
WHERE old_category.category_name = 'Do uong'
   OR old_category.category_name = 'Đồ uống';

UPDATE menu_items mi
JOIN menu_categories old_category ON old_category.category_id = mi.category_id
JOIN menu_categories new_category ON new_category.category_name = 'Desserts'
SET mi.category_id = new_category.category_id
WHERE old_category.category_name = 'Trang mieng'
   OR old_category.category_name = 'Tráng miệng'
   OR old_category.category_name = 'Dessert';

UPDATE menu_categories
SET deleted_at = CURRENT_TIMESTAMP
WHERE category_name IN (
    'Mon nong',
    'Món nóng',
    'Mi & Com',
    'Mì & Cơm',
    'Do uong',
    'Đồ uống',
    'Trang mieng',
    'Tráng miệng',
    'Dessert'
);

DROP TEMPORARY TABLE IF EXISTS tmp_more_menu_items;

CREATE TEMPORARY TABLE tmp_more_menu_items (
    category_name VARCHAR(100) NOT NULL,
    item_name VARCHAR(150) NOT NULL,
    description TEXT NULL,
    image_url VARCHAR(500) NULL,
    price DECIMAL(12, 2) NOT NULL
);

INSERT INTO tmp_more_menu_items (category_name, item_name, description, image_url, price)
VALUES
    ('Sushi & Sashimi', 'Dragon Roll', 'Rice roll with grilled eel, avocado, cucumber and kabayaki sauce.', 'images/dragon-roll.png', 169000),
    ('Sushi & Sashimi', 'Spicy Tuna Roll', 'Spicy tuna roll with roasted sesame and Japanese mayo.', 'images/spicy-tuna-roll.png', 149000),
    ('Sushi & Sashimi', 'Unagi Nigiri', 'Grilled Japanese eel nigiri with sweet savory sauce.', 'images/unagi-nigiri.png', 139000),
    ('Sushi & Sashimi', 'Hotate Sashimi', 'Chilled Japanese scallop sashimi with a clean sweet finish.', 'images/hotate-sashimi.png', 189000),
    ('Sushi & Sashimi', 'Ikura Gunkan', 'Fresh salmon roe gunkan with crisp nori and sushi rice.', 'images/ikura-gunkan.png', 179000),
    ('Sushi & Sashimi', 'Tamago Nigiri', 'Japanese omelet nigiri with a soft sweet flavor.', 'images/tamago-nigiri.png', 69000),
    ('Hot Kitchen', 'Chicken Karaage', 'Japanese fried chicken served with lemon mayo sauce.', 'images/chicken-karaage.png', 109000),
    ('Hot Kitchen', 'Pork Gyoza', 'Pan-fried pork and vegetable dumplings with ponzu sauce.', 'images/pork-gyoza.png', 99000),
    ('Hot Kitchen', 'Takoyaki', 'Osaka octopus balls topped with bonito flakes and takoyaki sauce.', 'images/takoyaki.png', 89000),
    ('Hot Kitchen', 'Salmon Teriyaki', 'Pan-seared salmon with teriyaki sauce and vegetables.', 'images/salmon-teriyaki.png', 199000),
    ('Hot Kitchen', 'Beef Yakiniku', 'Japanese grilled beef with rich yakiniku sauce.', 'images/beef-yakiniku.png', 189000),
    ('Hot Kitchen', 'Miso Soup', 'Warm miso soup with tofu, wakame and spring onion.', 'images/miso-soup.png', 49000),
    ('Rice & Noodles', 'Shoyu Ramen', 'Soy sauce ramen with soft-boiled egg and chashu pork.', 'images/shoyu-ramen.png', 139000),
    ('Rice & Noodles', 'Miso Ramen', 'Miso ramen with corn, nori and chashu pork.', 'images/miso-ramen.png', 149000),
    ('Rice & Noodles', 'Chicken Katsu Curry', 'Japanese curry rice with crispy chicken katsu.', 'images/chicken-katsu-curry.png', 159000),
    ('Rice & Noodles', 'Unagi Don', 'Grilled eel rice bowl with kabayaki sauce and sesame.', 'images/unagi-don.png', 229000),
    ('Rice & Noodles', 'Chirashi Don', 'Sushi rice bowl with assorted sashimi and salmon roe.', 'images/chirashi-don.png', 239000),
    ('Beverages', 'Yuzu Soda', 'Refreshing Japanese yuzu soda with a citrus finish.', 'images/yuzu-soda.png', 59000),
    ('Beverages', 'Genmaicha Tea', 'Japanese roasted rice green tea with a warm aroma.', 'images/genmaicha-tea.png', 49000),
    ('Beverages', 'Japanese Lemon Tea', 'Japanese-style lemon tea with balanced sweet and sour notes.', 'images/japanese-lemon-tea.png', 55000),
    ('Beverages', 'Ramune', 'Classic Japanese marble soda with fruit flavor.', 'images/ramune.png', 65000),
    ('Desserts', 'Dorayaki', 'Soft Japanese pancake filled with red bean paste.', 'images/dorayaki.png', 69000),
    ('Desserts', 'Matcha Pudding', 'Smooth matcha pudding with a gentle green tea aroma.', 'images/matcha-pudding.png', 79000),
    ('Desserts', 'Taiyaki', 'Fish-shaped Japanese cake with custard or red bean filling.', 'images/taiyaki.png', 75000);

UPDATE menu_items mi
JOIN tmp_more_menu_items tmp ON tmp.item_name = mi.item_name
JOIN menu_categories mc ON mc.category_name = tmp.category_name
SET
    mi.category_id = mc.category_id,
    mi.description = tmp.description,
    mi.image_url = tmp.image_url,
    mi.price = tmp.price,
    mi.is_available = 1,
    mi.deleted_at = NULL;

INSERT INTO menu_items (
    category_id,
    item_name,
    description,
    image_url,
    price,
    is_available,
    deleted_at
)
SELECT
    mc.category_id,
    tmp.item_name,
    tmp.description,
    tmp.image_url,
    tmp.price,
    1,
    NULL
FROM tmp_more_menu_items tmp
JOIN menu_categories mc ON mc.category_name = tmp.category_name
WHERE NOT EXISTS (
    SELECT 1
    FROM menu_items mi
    WHERE mi.item_name = tmp.item_name
);

DROP TEMPORARY TABLE IF EXISTS tmp_more_menu_items;

SELECT
    mc.category_name,
    mi.menu_item_id,
    mi.item_name,
    mi.price,
    mi.is_available,
    mi.deleted_at
FROM menu_items mi
JOIN menu_categories mc ON mc.category_id = mi.category_id
WHERE mi.item_name IN (
    'Dragon Roll',
    'Spicy Tuna Roll',
    'Unagi Nigiri',
    'Hotate Sashimi',
    'Ikura Gunkan',
    'Tamago Nigiri',
    'Chicken Karaage',
    'Pork Gyoza',
    'Takoyaki',
    'Salmon Teriyaki',
    'Beef Yakiniku',
    'Miso Soup',
    'Shoyu Ramen',
    'Miso Ramen',
    'Chicken Katsu Curry',
    'Unagi Don',
    'Chirashi Don',
    'Yuzu Soda',
    'Genmaicha Tea',
    'Japanese Lemon Tea',
    'Ramune',
    'Dorayaki',
    'Matcha Pudding',
    'Taiyaki'
)
ORDER BY mc.category_name, mi.item_name;
