using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace _290526_SushiRestaurantManagement_BE.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly RestaurantSystemDbContext _context;

        public ForgotPasswordModel(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string Phone { get; set; } = "";

        public string? Message { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Phone == Phone && s.IsActive == true);

            if (staff == null)
            {
                Message = "Số điện thoại không tồn tại.";
                return Page();
            }

            return RedirectToPage("/Auth/ResetPassword", new { phone = Phone });
        }
    }
}
