using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace _290526_SushiRestaurantManagement_BE.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly RestaurantSystemDbContext _context;

        public ResetPasswordModel(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ResetPasswordInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet(string phone)
        {
            Input.Phone = phone?.Trim() ?? "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var phone = Input.Phone.Trim();

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s =>
                    s.Phone == phone &&
                    s.IsActive);

            if (staff == null)
            {
                ErrorMessage = "Không tìm thấy tài khoản.";
                return Page();
            }

            staff.PasswordHash = global::BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToPage("/Auth/Login");
        }
    }

    public class ResetPasswordInput
    {
        [Required]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
