using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class IndexModel : AdminPageModel
    {
        private readonly IBookingService _bookingService;
        private readonly ITableAvailabilityService _tableAvailabilityService;

        public IndexModel(IBookingService bookingService, ITableAvailabilityService tableAvailabilityService)
        {
            _bookingService = bookingService;
            _tableAvailabilityService = tableAvailabilityService;
        }

        [BindProperty(SupportsGet = true)]
        public DateOnly? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? EndDate { get; set; }

        public int TotalBookingsCount { get; set; }
        public int AvailableTablesCount { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Luôn kh?i t?o danh sách r?ng ?? tránh l?i Null ? giao di?n .cshtml
        public List<BusinessObjects.Models.Booking> FilteredBookings { get; set; } = new List<BusinessObjects.Models.Booking>();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var nowTime = TimeOnly.FromDateTime(DateTime.Now);

            // N?u ch?a ch?n ngày, m?c ??nh l?y t? ??u tháng hi?n t?i ??n hôm nay
            if (StartDate == null) StartDate = new DateOnly(today.Year, today.Month, 1);
            if (EndDate == null) EndDate = today;

            // Ki?m tra an toàn h? th?ng d?ch v?
            if (_bookingService == null || _tableAvailabilityService == null) return;

            // 1. L?y danh sách t?ng an toàn
            var allBookings = await _bookingService.GetAllBookingsAsync();

            // Kh?i t?o danh sách s?ch ?? l?u các booking h?p l?
            FilteredBookings = new List<BusinessObjects.Models.Booking>();

            if (allBookings != null)
            {
                // Th?c hi?n l?c theo kho?ng ngày ?ã ch?n
                var tempBookings = allBookings
                    .Where(b => b.BookingDate >= StartDate.Value && b.BookingDate <= EndDate.Value)
                    .OrderByDescending(b => b.BookingDate)
                    .ThenByDescending(b => b.BookingTime);

                foreach (var b in tempBookings)
                {
                    // N?u b?n ghi trong DB b? l?i hoàn toàn d?n ??n object null, b? qua ngay
                    if (b == null) continue;

                    FilteredBookings.Add(b);
                }
            }

            // Gán s? l??ng ph?n t? ??m ???c
            TotalBookingsCount = FilteredBookings.Count;

            // 2. Tính s? l??ng bàn tr?ng hi?n t?i
            try
            {
                AvailableTablesCount = await _tableAvailabilityService.GetAvailableTableCountAsync(today, nowTime);
            }
            catch
            {
                AvailableTablesCount = 0; // Tránh s?p n?u service bàn tr?ng l?i
            }

            // 3. Tính toán doanh thu tích l?y - B?O V? TUY?T ??I KH?I BOOKING L?I
            decimal revenueSum = 0;
            foreach (var booking in FilteredBookings)
            {
                // ??t toàn b? block x? lý 1 dòng booking vào try-catch ??c l?p
                try
                {
                    if (booking != null)
                    {
                        var orderHistory = await _bookingService.GetBookingOrderHistoryAsync(booking.BookingId);
                        if (orderHistory != null)
                        {
                            revenueSum += (decimal)orderHistory.TemporaryTotal;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Khi booking này b? l?i c? s? d? li?u ho?c không tìm th?y orderhistory,
                    // L?nh 'continue' s? kích ho?t ?? b? qua dòng l?i này và nh?y sang booking ti?p theo ngay l?p t?c.
                    System.Diagnostics.Debug.WriteLine($"L?i t?i booking #{booking?.BookingId}: {ex.Message}");
                    continue;
                }
            }
            MonthlyRevenue = revenueSum;
        }
    }
}