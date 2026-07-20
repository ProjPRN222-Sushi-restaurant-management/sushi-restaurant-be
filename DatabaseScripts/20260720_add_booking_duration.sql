-- =========================================================
-- Thêm thời lượng dùng bữa (phút) cho mỗi lượt đặt bàn.
-- Dùng để kiểm tra TRÙNG KHOẢNG THỜI GIAN thay vì so đúng 1 mốc giờ.
-- Chạy 1 lần trên RestaurantSystemDB.
-- =========================================================
USE RestaurantSystemDB;

ALTER TABLE bookings
    ADD COLUMN duration_minutes INT NOT NULL DEFAULT 90 AFTER booking_time;

-- (Tuỳ chọn) đặt lại thời lượng mặc định cho các booking cũ nếu cần:
-- UPDATE bookings SET duration_minutes = 90 WHERE duration_minutes IS NULL OR duration_minutes <= 0;
