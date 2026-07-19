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
