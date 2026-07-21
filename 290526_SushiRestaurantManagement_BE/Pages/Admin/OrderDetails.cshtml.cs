using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using BusinessObjects.Models;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class OrderDetailsModel : AdminPageModel
    {
        private readonly IOrderService _orderService;

        public OrderDetailsModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public BusinessObjects.Models.Order? Order { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Print { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Order = await _orderService.GetOrderByIdAsync(id);
            if (Order == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostIssueInvoiceAsync(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            ApplyInvoiceIssuer(order);
            await _orderService.UpdateOrderAsync(order);

            return RedirectToPage("/Admin/OrderDetails", new
            {
                id = order.OrderId,
                print = true
            });
        }

        private void ApplyInvoiceIssuer(BusinessObjects.Models.Order order)
        {
            if (order.InvoiceIssuedAt.HasValue)
            {
                return;
            }

            order.InvoiceIssuedAt = DateTime.Now;

            if (order.ReceivedStaffId.HasValue ||
                !string.IsNullOrWhiteSpace(order.ReceivedStaffName))
            {
                order.InvoiceStaffId = order.ReceivedStaffId;
                order.InvoiceStaffName =
                    order.ReceivedStaff?.FullName ??
                    order.ReceivedStaffName ??
                    "Khong xac dinh";
                return;
            }

            order.InvoiceStaffName = HttpContext.Session.GetString("StaffName") ?? "Khong xac dinh";

            if (long.TryParse(HttpContext.Session.GetString("StaffId"), out var staffId) &&
                staffId > 0)
            {
                order.InvoiceStaffId = staffId;
                return;
            }

            order.InvoiceStaffId = null;
        }
    }
}