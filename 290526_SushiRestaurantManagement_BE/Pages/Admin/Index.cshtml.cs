using _290526_SushiRestaurantManagement_BE.Helpers;
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

        public PaginatedList<BusinessObjects.Models.Booking> FilteredBookings { get; set; } = new(new List<BusinessObjects.Models.Booking>(), 0, 1, 10);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var nowTime = TimeOnly.FromDateTime(DateTime.Now);

            // N?u ch?a ch?n ng�y, m?c ??nh l?y t? ??u th�ng hi?n t?i ??n h�m nay
            if (StartDate == null) StartDate = new DateOnly(today.Year, today.Month, 1);
            if (EndDate == null) EndDate = today;

            // Ki?m tra an to�n h? th?ng d?ch v?
            if (_bookingService == null || _tableAvailabilityService == null) return;

            // 1. L?y danh s�ch t?ng an to�n
            var allBookings = await _bookingService.GetAllBookingsAsync();

            // Kh?i t?o danh s�ch s?ch ?? l?u c�c booking h?p l?
            if (allBookings != null)
            {
                // Th?c hi?n l?c theo kho?ng ng�y ?� ch?n
                var tempBookings = allBookings
                    .Where(b => b.BookingDate >= StartDate.Value && b.BookingDate <= EndDate.Value)
                    .OrderByDescending(b => b.BookingDate)
                    .ThenByDescending(b => b.BookingTime)
                    .ToList();

                FilteredBookings = PaginatedList<BusinessObjects.Models.Booking>.Create(tempBookings, PageNumber, PageSize);
            }

            // G�n s? l??ng ph?n t? ??m ???c
            TotalBookingsCount = FilteredBookings.TotalItems;

            // 2. T�nh s? l??ng b�n tr?ng hi?n t?i
            try
            {
                AvailableTablesCount = await _tableAvailabilityService.GetAvailableTableCountAsync(today, nowTime);
            }
            catch
            {
                AvailableTablesCount = 0; // Tr�nh s?p n?u service b�n tr?ng l?i
            }

            // 3. T�nh to�n doanh thu t�ch l?y - B?O V? TUY?T ??I KH?I BOOKING L?I
            decimal revenueSum = 0;
            var allFilteredBookings = FilteredBookings.Items;
            foreach (var booking in allFilteredBookings)
            {
                // ??t to�n b? block x? l� 1 d�ng booking v�o try-catch ??c l?p
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
                    // Khi booking n�y b? l?i c? s? d? li?u ho?c kh�ng t�m th?y orderhistory,
                    // L?nh 'continue' s? k�ch ho?t ?? b? qua d�ng l?i n�y v� nh?y sang booking ti?p theo ngay l?p t?c.
                    System.Diagnostics.Debug.WriteLine($"L?i t?i booking #{booking?.BookingId}: {ex.Message}");
                    continue;
                }
            }
            MonthlyRevenue = revenueSum;
        }
    }
}