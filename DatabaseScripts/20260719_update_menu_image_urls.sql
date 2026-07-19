-- =========================================================
-- Đồng bộ đường dẫn ảnh cho 12 món seed trong bảng menu_items.
-- Ảnh vật lý đặt tại: wwwroot/images/<tên>.png
-- Lưu ý: Menu/Index đã map ảnh theo TÊN MÓN trong code (ưu tiên cao hơn),
--        nên script này chỉ để DB nhất quán / dự phòng. Idempotent.
-- =========================================================
USE RestaurantSystemDB;

UPDATE menu_items SET image_url = 'images/salmon-sushi.png'    WHERE item_name = 'Salmon Sushi';
UPDATE menu_items SET image_url = 'images/tuna-sushi.png'      WHERE item_name = 'Tuna Sushi';
UPDATE menu_items SET image_url = 'images/salmon-sashimi.png'  WHERE item_name = 'Salmon Sashimi';
UPDATE menu_items SET image_url = 'images/tuna-sashimi.png'    WHERE item_name = 'Tuna Sashimi';
UPDATE menu_items SET image_url = 'images/ebi-nigiri.png'      WHERE item_name = 'Ebi Nigiri';
UPDATE menu_items SET image_url = 'images/California-roll.png' WHERE item_name = 'California Roll';
UPDATE menu_items SET image_url = 'images/tonkotsu-ramen.png'  WHERE item_name = 'Tonkotsu Ramen';
UPDATE menu_items SET image_url = 'images/tempura-udon.png'    WHERE item_name = 'Tempura Udon';
UPDATE menu_items SET image_url = 'images/shrimp-tempura.png'  WHERE item_name = 'Shrimp Tempura';
UPDATE menu_items SET image_url = 'images/match-latte.png'     WHERE item_name = 'Matcha Latte';
UPDATE menu_items SET image_url = 'images/sakura-tea.png'      WHERE item_name = 'Sakura Tea';
UPDATE menu_items SET image_url = 'images/mochi-ice-cream.png' WHERE item_name = 'Mochi Ice Cream';

SELECT menu_item_id, item_name, image_url FROM menu_items ORDER BY menu_item_id;
