using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

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
            Input.Phone = phone;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Phone == Input.Phone && s.IsActive == true);

            if (staff == null)
            {
                ErrorMessage = "Account not found.";
                return Page();
            }

            staff.PasswordHash = HashPassword(Input.NewPassword);

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. Please login.";
            return RedirectToPage("/Auth/Login");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class ResetPasswordInput
    {
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password does not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
