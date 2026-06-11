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

            var phone = Input.Phone.Trim();

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s =>
                    s.Phone == phone &&
                    (s.IsActive ?? false));

            if (staff == null || string.IsNullOrWhiteSpace(staff.PasswordHash))
            {
                ErrorMessage = "Invalid phone or password.";
                return Page();
            }

            var hash = staff.PasswordHash.Trim();

            if (hash.StartsWith("$2y$"))
            {
                hash = "$2a$" + hash.Substring(4);
            }

            bool isValidPassword;

            try
            {
                isValidPassword = global::BCrypt.Net.BCrypt.Verify(Input.Password, hash);
            }
            catch
            {
                ErrorMessage = "Password hash in database is invalid.";
                return Page();
            }

            if (!isValidPassword)
            {
                ErrorMessage = "Invalid phone or password.";
                return Page();
            }

            HttpContext.Session.SetString("StaffId", staff.StaffId.ToString());
            HttpContext.Session.SetString("StaffName", staff.FullName ?? "");
            HttpContext.Session.SetString("StaffPhone", staff.Phone ?? "");

            return RedirectToPage("/Booking/Create");
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