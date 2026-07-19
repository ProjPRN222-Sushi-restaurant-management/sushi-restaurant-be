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

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var phone = Input.Phone.Trim();

            // 1. KI?M TRA T�I KHO?N ADMIN FIX C?NG TR??C
            var adminPhone = _configuration["AdminAccount:Phone"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var adminName = _configuration["AdminAccount:FullName"] ?? "Admin";

            if (phone == adminPhone && Input.Password == adminPassword)
            {
                // L?u session ph�n quy?n Admin
                HttpContext.Session.SetString("StaffId", "0"); // ID m?c ??nh cho Admin c?ng
                HttpContext.Session.SetString("StaffName", adminName);
                HttpContext.Session.SetString("StaffPhone", phone);
                HttpContext.Session.SetString("StaffRole", "Admin"); // ?�nh d?u vai tr�

                return RedirectToPage("/Admin/Index"); // ?i?u h??ng th?ng v�o Dashboard Admin
            }

            // 2. N?U KH�NG PH?I ADMIN TH� KI?M TRA STAFF TRONG DATABASE NH? C?
            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Phone == phone && s.IsActive);

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
            HttpContext.Session.SetString("StaffRole", "Staff"); // T�i kho?n nh�n vi�n th�ng th??ng

            return RedirectToPage("/Booking/Create");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Auth/Login");
        }

        public class LoginInput
        {
            [Required(ErrorMessage = "Phone is required")]
            public string Phone { get; set; } = "";

            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; } = "";
        }
    }
}