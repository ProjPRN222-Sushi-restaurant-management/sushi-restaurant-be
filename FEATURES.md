# Các Tính Năng Mới Được Thêm

## 1. Xem Chi Tiết Số Lượng Bàn Được Đặt & Trạng Thái Bàn

### Trang quản lý tình trạng bàn: `/Booking/Availability`
- Hiển thị tổng số bàn trong nhà hàng
- Số bàn được đặt theo ngày
- Số bàn trống
- Tỉ lệ chiếm chỗ (%)
- Tình trạng bàn theo từng khung giờ (mỗi 30 phút)
- Danh sách chi tiết các đặt bàn trong ngày

### Service: `ITableAvailabilityService`
**Vị trí:** `Services/Interfaces/ITableAvailabilityService.cs`
**Triển khai:** `Services/Implementations/TableAvailabilityService.cs`

**Các phương thức:**
- `GetAvailableTablesAsync()` - Lấy danh sách bàn trống cho ngày/giờ cụ thể
- `GetBookedTableCountAsync()` - Lấy số bàn được đặt
- `GetAvailableTableCountAsync()` - Lấy số bàn trống
- `GetBookingsByDateAsync()` - Lấy tất cả đặt bàn trong ngày
- `GetTableBookingCountByDateTimeAsync()` - Lấy số lượt đặt theo bàn

---

## 2. Hệ Thống Auto-Update Trạng Thái Bàn

### Background Service: `TableStatusUpdateHostedService`
**Vị trí:** `Services/Implementations/TableStatusUpdateHostedService.cs`

**Chức năng:**
- Chạy mỗi 5 phút tự động
- Cập nhật trạng thái booking từ PENDING → COMPLETED khi hết thời gian
- Gửi notification nhắc nhở 30 phút trước khi đến
- Xử lý lỗi không làm gián đoạn quy trình đặt bàn

**Quy trình:**
1. Kiểm tra các booking đã quá thời gian → cập nhật thành COMPLETED
2. Kiểm tra các booking sắp tới (trong 30 phút) → gửi nhắc nhở qua Zalo

---

## 3. Gửi Notification Qua Zalo

### Service: `INotificationService`
**Vị trí:** `Services/Interfaces/INotificationService.cs`
**Triển khai:** `Services/Implementations/NotificationService.cs`

**Các phương thức:**
- `SendBookingConfirmationAsync()` - Gửi xác nhận đặt bàn
- `SendBookingStatusChangeAsync()` - Gửi thông báo khi trạng thái thay đổi
- `SendBookingReminderAsync()` - Gửi nhắc nhở 30 phút trước

**Cấu hình Zalo:**
```json
{
  "Zalo": {
    "ApiUrl": "https://api.zalo.me/v2.0",
    "AccessToken": "your_access_token_here"
  }
}
```

**Tích hợp:**
- Được gọi tự động từ `TableStatusUpdateHostedService`
- Có thể gọi thủ công từ Booking success page
- Xử lý lỗi an toàn - không làm fail quy trình đặt bàn

---

## 4. Cải Tiến Trang Booking Success

### Hiển thị:
- Mã booking (ID)
- Thời gian xác nhận
- Thông báo gửi qua Zalo

---

## 5. Forgot Password (Đã Có Sẵn)

### Trang:
- `/Auth/ForgotPassword` - Nhập số điện thoại
- `/Auth/ResetPassword` - Đặt mật khẩu mới

**Chức năng:**
- Xác nhận số điện thoại tồn tại
- Cho phép reset mật khẩu
- Bảo mật password bằng BCrypt

---

## Cài Đặt & Sử Dụng

### 1. Đăng ký Services trong Startup (Program.cs)
```csharp
// Table Availability Service
builder.Services.AddScoped<ITableAvailabilityService, TableAvailabilityService>();

// Notification Service  
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background Service
builder.Services.AddHostedService<TableStatusUpdateHostedService>();

// HttpClient
builder.Services.AddHttpClient();
```

### 2. Cấu hình appsettings.json
```json
{
  "Zalo": {
    "ApiUrl": "https://api.zalo.me/v2.0",
    "AccessToken": "your_access_token"
  }
}
```

### 3. Sử dụng Services
```csharp
// Inject services
private readonly ITableAvailabilityService _tableAvailability;
private readonly INotificationService _notification;

// Kiểm tra bàn trống
var availableTables = await _tableAvailability.GetAvailableTablesAsync(date, time);
var availableCount = await _tableAvailability.GetAvailableTableCountAsync(date, time);

// Gửi notification
await _notification.SendBookingConfirmationAsync(phone, bookingId, dateTime, name);
```

---

## URL Tham Khảo

- **Xem tình trạng bàn:** `/Booking/Availability`
- **Trang success:** `/Booking/Success?id={bookingId}`
- **Menu đặt hàng:** `/Menu/Index?bookingId={bookingId}`
- **Quên mật khẩu:** `/Auth/ForgotPassword`

---

## Ghi Chú

- Background service chạy mỗi 5 phút tự động
- Zalo API là tuỳ chọn - không bắt buộc để đặt bàn hoạt động
- Tất cả thời gian (Date, Time) được lưu theo múi giờ server
- TableStatusEnum: AVAILABLE, BOOKED, OCCUPIED
- BookingStatusEnum: PENDING, COMPLETED, CANCELLED
