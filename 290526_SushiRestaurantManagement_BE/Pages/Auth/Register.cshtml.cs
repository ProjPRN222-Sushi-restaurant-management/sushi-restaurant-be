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
                ModelState.AddModelError("Input.Phone", "Phone already exists.");
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

            TempData["Success"] = "Register successfully. Please login.";
            return RedirectToPage("/Auth/Login");
        }
    }

    public class RegisterInput
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(Password), ErrorMessage = "Password does not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
