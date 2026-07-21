using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.Policies;

namespace _290526_SushiRestaurantManagement_BE.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IBookingService _bookingService;
        private readonly IRestaurantTableService _tableService;

        public CheckoutModel(
            IOrderService orderService,
            IBookingService bookingService,
            IRestaurantTableService tableService)
        {
            _orderService = orderService;
            _bookingService = bookingService;
            _tableService = tableService;
        }

        public List<CartItemViewModel> CartItems { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public int? OrderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Print { get; set; }

        public BusinessObjects.Models.Order? SavedOrder { get; set; }

        public MembershipLevelEnum CustomerMembershipLevel { get; set; }

        public int CustomerLoyaltyPoints { get; set; }

        public decimal SubtotalAmount =>
            SavedOrder != null && SavedOrder.SubtotalAmount > 0
                ? SavedOrder.SubtotalAmount
                : CartItems.Sum(x => x.Total);

        public decimal DiscountPercent =>
            SavedOrder?.DiscountPercent ??
            LoyaltyPolicy.GetDiscountPercent(CustomerMembershipLevel);

        public decimal DiscountAmount =>
            SavedOrder?.DiscountAmount ??
            LoyaltyPolicy.CalculateDiscountAmount(SubtotalAmount, CustomerMembershipLevel);

        public decimal TotalAmount =>
            SavedOrder?.TotalAmount ??
            LoyaltyPolicy.CalculatePayableAmount(SubtotalAmount, CustomerMembershipLevel);

        public int EarnedPoints =>
            SavedOrder != null && SavedOrder.EarnedLoyaltyPoints > 0
                ? SavedOrder.EarnedLoyaltyPoints
                : LoyaltyPolicy.CalculateEarnedPoints(TotalAmount);

        public async Task<IActionResult> OnGetAsync()
        {
            if (OrderId.HasValue)
            {
                try
                {
                    SavedOrder = await _orderService.GetOrderByIdAsync(OrderId.Value);
                    CustomerMembershipLevel = SavedOrder.MembershipLevelApplied;
                    CustomerLoyaltyPoints =
                        SavedOrder.Customer?.LoyaltyPoints ??
                        SavedOrder.Booking?.Customer?.LoyaltyPoints ??
                        0;
                    CartItems = SavedOrder.OrderItems.Select(x => new CartItemViewModel
                    {
                        MenuItemId = x.MenuItemId,
                        ItemName = x.MenuItem?.ItemName ?? $"Món #{x.MenuItemId}",
                        UnitPrice = x.UnitPrice,
                        Quantity = x.Quantity,
                        Note = x.Note
                    }).ToList();
                }
                catch (KeyNotFoundException)
                {
                    TempData["Error"] = "Không tìm thấy order vừa gửi.";
                    return RedirectToPage("/Cart/Index");
                }

                return Page();
            }

            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];
            await LoadCustomerLoyaltyContextAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            if (!CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống.";
                return RedirectToPage("/Cart/Index");
            }

            long? bookingId = null;
            long? tableId = null;

            if (long.TryParse(HttpContext.Session.GetString("BOOKING_ID"), out var bId))
                bookingId = bId;

            if (long.TryParse(HttpContext.Session.GetString("TABLE_ID"), out var tId))
                tableId = tId;

            BusinessObjects.Models.Booking? booking = null;
            if (bookingId.HasValue)
            {
                booking = await _bookingService.GetBookingByIdAsync(bookingId.Value);
                tableId ??= booking?.TableId;
            }

            var now = DateTime.Now;
            var membershipLevel = booking?.Customer?.MembershipLevel ?? MembershipLevelEnum.NONE;
            var subtotalAmount = CartItems.Sum(x => x.Total);
            var discountPercent = LoyaltyPolicy.GetDiscountPercent(membershipLevel);
            var discountAmount = LoyaltyPolicy.CalculateDiscountAmount(
                subtotalAmount,
                membershipLevel);
            var totalAmount = LoyaltyPolicy.CalculatePayableAmount(
                subtotalAmount,
                membershipLevel);
            var earnedPoints = LoyaltyPolicy.CalculateEarnedPoints(totalAmount);
            var bookingStatus = booking != null
                ? BookingStatusPolicy.GetActiveStatus(booking, now)
                : BookingStatusEnum.PREPARING;
            var orderStatus = BookingStatusPolicy.ToOrderStatus(bookingStatus);

            var order = new BusinessObjects.Models.Order
            {
                BookingId = bookingId,
                CustomerId = booking?.CustomerId,
                TableId = tableId,
                SubtotalAmount = subtotalAmount,
                MembershipLevelApplied = membershipLevel,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                EarnedLoyaltyPoints = earnedPoints,
                OrderStatus = orderStatus,
                CreatedAt = now,
                ReceivedAt = now,
                ReceivedStaffId = TryGetCurrentStaffId(),
                ReceivedStaffName = HttpContext.Session.GetString("StaffName") ?? "Không xác định",
                OrderItems = CartItems.Select(x => new OrderItem
                {
                    MenuItemId = x.MenuItemId,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.Total,
                    Note = x.Note
                }).ToList()
            };

            await _orderService.AddOrderAsync(order);
            await _orderService.SaveChangesAsync();

            if (bookingId.HasValue)
            {
                await _bookingService.UpdateBookingStatusAsync(
                    bookingId.Value,
                    bookingStatus);
            }

            if (tableId.HasValue)
            {
                await _tableService.UpdateTableStatusAsync(
                    tableId.Value,
                    BookingStatusPolicy.ToTableStatus(bookingStatus));
            }

            HttpContext.Session.Remove("CART");

            TempData["OrderSuccess"] = $"Gửi order thành công! Mã order #{order.OrderId}";

            return RedirectToPage("/Cart/Checkout", new
            {
                orderId = order.OrderId,
                print = true
            });
        }

        private long? TryGetCurrentStaffId()
        {
            if (long.TryParse(HttpContext.Session.GetString("StaffId"), out var staffId) &&
                staffId > 0)
            {
                return staffId;
            }

            return null;
        }

        private async Task LoadCustomerLoyaltyContextAsync()
        {
            if (!long.TryParse(HttpContext.Session.GetString("BOOKING_ID"), out var bookingId))
            {
                return;
            }

            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                CustomerMembershipLevel = booking.Customer?.MembershipLevel ?? MembershipLevelEnum.NONE;
                CustomerLoyaltyPoints = booking.Customer?.LoyaltyPoints ?? 0;
            }
            catch (KeyNotFoundException)
            {
                CustomerMembershipLevel = MembershipLevelEnum.NONE;
                CustomerLoyaltyPoints = 0;
            }
        }
    }
}
