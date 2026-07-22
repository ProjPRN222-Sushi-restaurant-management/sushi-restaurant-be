using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace _290526_SushiRestaurantManagement_BE.Pages.Auth
{
    public class RegisterModel(RestaurantSystemDbContext context) : PageModel
    {
        private readonly RestaurantSystemDbContext _context = context;

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            Input.Phone = Input.Phone.Trim();

            var existed = await _context.Staffs
                .AnyAsync(s => s.Phone == Input.Phone);

            if (existed)
            {
                ModelState.AddModelError("Input.Phone", "Số điện thoại đã tồn tại.");
                return Page();
            }

            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var staff = new Staff
            {
                FullName = Input.FullName.Trim(),
                Phone = Input.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToPage("/Auth/Login");
        }
    }

    public class RegisterInput
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
