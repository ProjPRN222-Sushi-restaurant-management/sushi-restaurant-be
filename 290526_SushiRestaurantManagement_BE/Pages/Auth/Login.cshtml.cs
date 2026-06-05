using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace _290526_SushiRestaurantManagement_BE.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly RestaurantSystemDbContext _context;

        public LoginModel(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s =>
                    s.Phone == Input.Phone &&
                    (s.IsActive ?? false));

            if (staff == null)
            {
                ErrorMessage = "Invalid phone or password.";
                return Page();
            }

            if (!BCrypt.Net.BCrypt.Verify(Input.Password, staff.PasswordHash))
            {
                ErrorMessage = "Invalid phone or password.";
                return Page();
            }

            HttpContext.Session.SetString("StaffId", staff.StaffId.ToString());
            HttpContext.Session.SetString("StaffName", staff.FullName);
            HttpContext.Session.SetString("StaffPhone", staff.Phone);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Auth/Login");
        }
    }

    public class LoginInput
    {
        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = "";
    }
}
