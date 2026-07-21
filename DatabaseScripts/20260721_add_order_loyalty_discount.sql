ALTER TABLE customers
    ADD COLUMN IF NOT EXISTS loyalty_points INT NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS membership_level VARCHAR(20) NOT NULL DEFAULT 'NONE';

UPDATE customers
SET membership_level = CASE
    WHEN loyalty_points >= 1000 THEN 'DIAMOND'
    WHEN loyalty_points >= 500 THEN 'GOLD'
    WHEN loyalty_points >= 100 THEN 'SILVER'
    ELSE 'NONE'
END;

ALTER TABLE orders
    ADD COLUMN IF NOT EXISTS subtotal_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS membership_level_applied VARCHAR(20) NOT NULL DEFAULT 'NONE',
    ADD COLUMN IF NOT EXISTS discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS earned_loyalty_points INT NOT NULL DEFAULT 0;

UPDATE orders
SET subtotal_amount = total_amount
WHERE subtotal_amount = 0;

UPDATE orders
SET earned_loyalty_points = FLOOR(total_amount / 10000)
WHERE earned_loyalty_points = 0
  AND order_status = 'COMPLETED';
