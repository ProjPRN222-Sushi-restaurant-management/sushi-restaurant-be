using BusinessObjects.Enums;

namespace DataAccessObjects.Requests
{
    public class CreateBookingRequest
    {
        public DateOnly BookingDate { get; set; }
        public TimeOnly BookingTime { get; set; }

        public int AdultCount { get; set; }
        public int ChildCount { get; set; }

        public TableTypeEnum TableType { get; set; }

        public string GuestName { get; set; } = "";
        public string GuestPhone { get; set; } = "";
        public string? Note{ get; set; }
    }
}
