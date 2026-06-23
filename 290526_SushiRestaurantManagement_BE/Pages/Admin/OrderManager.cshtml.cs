using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class OrderManagerModel : AdminPageModel
    {
        private readonly IOrderService _orderService;

        public OrderManagerModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderStatusEnum? StatusFilter { get; set; }

        public PaginatedList<Order> Orders { get; set; } = new(new List<Order>(), 0, 1, 10);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var allOrders = await _orderService.GetAllOrdersAsync();
            var query = allOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var trimmed = SearchString.Trim();
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    o.BookingId.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    o.TableId.ToString().Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    (o.Customer != null && o.Customer.Phone != null && o.Customer.Phone.Contains(trimmed, StringComparison.OrdinalIgnoreCase)) ||
                    (o.Customer != null && o.Customer.FullName != null && o.Customer.FullName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(o => o.OrderStatus == StatusFilter.Value);
            }

            var orderedOrders = query.OrderByDescending(o => o.OrderId).ToList();
            Orders = PaginatedList<Order>.Create(orderedOrders, PageNumber, PageSize);
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, OrderStatusEnum status)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = status;
            if (status == OrderStatusEnum.COMPLETED)
            {
                order.CompletedAt = DateTime.Now;
            }

            await _orderService.UpdateOrderAsync(order);
            return RedirectToPage("/Admin/OrderManager");
        }
    }
}
