using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class OrderDetailsModel : AdminPageModel
    {
        private readonly IOrderService _orderService;

        public OrderDetailsModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Order = await _orderService.GetOrderByIdAsync(id);
            if (Order == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
