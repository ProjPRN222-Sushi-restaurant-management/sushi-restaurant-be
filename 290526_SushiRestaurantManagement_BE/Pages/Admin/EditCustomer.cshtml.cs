using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class EditCustomerModel : PageModel
    {
        private readonly ICustomerService _customerService;

        public EditCustomerModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [BindProperty]
        public Customer CustomerInfo { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            CustomerInfo = customer;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _customerService.UpdateCustomerAsync(CustomerInfo);
                TempData["Success"] = "Customer updated successfully.";
                return RedirectToPage("/Admin/CustomerManager");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to update customer: " + ex.Message);
                return Page();
            }
        }
    }
}
