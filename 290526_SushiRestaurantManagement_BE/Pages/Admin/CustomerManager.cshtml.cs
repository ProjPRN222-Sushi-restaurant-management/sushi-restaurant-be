using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class CustomerManagerModel : PageModel
    {
        private readonly ICustomerService _customerService;

        public CustomerManagerModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public PaginatedList<Customer> CustomerList { get; set; } = new(new List<Customer>(), 0, 1, 10);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var allCustomers = await _customerService.GetAllCustomersAsync();
            var query = allCustomers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                query = query.Where(c =>
                    (c.FullName != null && c.FullName.Contains(SearchString.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                    (c.Phone != null && c.Phone.Contains(SearchString.Trim(), StringComparison.OrdinalIgnoreCase)));
            }

            var orderedCustomers = query.OrderByDescending(c => c.CustomerId).ToList();
            CustomerList = PaginatedList<Customer>.Create(orderedCustomers, PageNumber, PageSize);
        }

        public async Task<IActionResult> OnPostAddCustomerAsync(string NewFullName, string NewPhone)
        {
            if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPhone))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ họ tên và số điện thoại.";
                return RedirectToPage();
            }

            try
            {
                var customer = new Customer
                {
                    FullName = NewFullName.Trim(),
                    Phone = NewPhone.Trim(),
                    MembershipLevel = BusinessObjects.Enums.MembershipLevelEnum.NONE,
                    LoyaltyPoints = 0,
                    CreatedAt = DateTime.Now
                };

                await _customerService.AddCustomerAsync(customer);
                TempData["Success"] = "Tạo khách hàng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tạo khách hàng: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                TempData["Success"] = "Xóa khách hàng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể xóa khách hàng: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
