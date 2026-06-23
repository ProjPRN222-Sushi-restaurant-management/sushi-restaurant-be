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

        public List<Customer> CustomerList { get; set; } = new();

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

            CustomerList = query.OrderByDescending(c => c.CustomerId).ToList();
        }

        public async Task<IActionResult> OnPostAddCustomerAsync(string NewFullName, string NewPhone)
        {
            if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPhone))
            {
                TempData["Error"] = "Please provide both full name and phone number.";
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
                TempData["Success"] = "Customer created successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to create customer: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to delete customer: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
