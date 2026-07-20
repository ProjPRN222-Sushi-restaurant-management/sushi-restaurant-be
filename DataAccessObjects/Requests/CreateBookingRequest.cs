using BusinessObjects.Enums;

namespace DataAccessObjects.Requests
{
    public class CreateBookingRequest
    {
        public DateOnly BookingDate { get; set; }
        public TimeOnly BookingTime { get; set; }

        // Thời lượng dùng bữa (phút) — dùng để kiểm tra trùng khoảng thời gian
        public int DurationMinutes { get; set; } = 90;

        // Bàn được chọn thủ công (null = để hệ thống tự gán bàn phù hợp)
        public long? TableId { get; set; }

        public int AdultCount { get; set; }
        public int ChildCount { get; set; }

        public TableTypeEnum TableType { get; set; }

        public string GuestName { get; set; } = "";
        public string GuestPhone { get; set; } = "";
        public string? Note{ get; set; }
    }
}
