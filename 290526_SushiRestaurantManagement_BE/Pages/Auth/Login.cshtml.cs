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
        private readonly IConfiguration _configuration;

        public LoginModel(RestaurantSystemDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
            {
                return Page();
            }

            var phone = Input.Phone.Trim();
            var adminPhone = _configuration["AdminAccount:Phone"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var adminName = _configuration["AdminAccount:FullName"] ?? "Quản trị viên";

            if (phone == adminPhone && Input.Password == adminPassword)
            {
                HttpContext.Session.SetString("StaffId", "0");
                HttpContext.Session.SetString("StaffName", adminName);
                HttpContext.Session.SetString("StaffPhone", phone);
                HttpContext.Session.SetString("StaffRole", "Admin");

                return RedirectToPage("/Admin/Index");
            }

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Phone == phone && s.IsActive);

            if (staff == null || string.IsNullOrWhiteSpace(staff.PasswordHash))
            {
                ErrorMessage = "Số điện thoại hoặc mật khẩu không đúng.";
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
                ErrorMessage = "Mật khẩu trong hệ thống không hợp lệ.";
                return Page();
            }

            if (!isValidPassword)
            {
                ErrorMessage = "Số điện thoại hoặc mật khẩu không đúng.";
                return Page();
            }

            HttpContext.Session.SetString("StaffId", staff.StaffId.ToString());
            HttpContext.Session.SetString("StaffName", staff.FullName ?? "");
            HttpContext.Session.SetString("StaffPhone", staff.Phone ?? "");
            HttpContext.Session.SetString("StaffRole", "Staff");

            return RedirectToPage("/Booking/Create");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Auth/Login");
        }

        public class LoginInput
        {
            [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
            public string Phone { get; set; } = "";

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            public string Password { get; set; } = "";
        }
    }
}
