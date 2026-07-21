ALTER TABLE orders
    ADD COLUMN received_staff_id BIGINT NULL AFTER completed_at,
    ADD COLUMN received_staff_name VARCHAR(150) NULL AFTER received_staff_id,
    ADD COLUMN received_at TIMESTAMP NULL AFTER received_staff_name;

CREATE INDEX idx_order_received_staff
ON orders(received_staff_id);

ALTER TABLE orders
ADD CONSTRAINT fk_order_received_staff
    FOREIGN KEY (received_staff_id)
    REFERENCES staffs(staff_id);