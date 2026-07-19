-- Fix MySQL "Data truncated for column 'booking_status'" after adding PREPARING.
-- EF maps these enum values as strings with max length 20, so keep the DB schema aligned.

ALTER TABLE bookings
    MODIFY booking_status VARCHAR(20) NOT NULL DEFAULT 'PENDING';

ALTER TABLE orders
    MODIFY order_status VARCHAR(20) NOT NULL DEFAULT 'PENDING';
