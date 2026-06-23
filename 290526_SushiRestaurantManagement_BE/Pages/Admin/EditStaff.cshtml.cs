using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObjects.Models;
using DataAccessObjects;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class EditStaffModel : PageModel
    {
        private readonly RestaurantSystemDbContext _context;

        public EditStaffModel(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Staff StaffInfo { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            StaffInfo = staff;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? NewPassword)
        {
            if (!ModelState.IsValid) return Page();

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                StaffInfo.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword.Trim());
            }

            _context.Attach(StaffInfo).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Update successfull!";
            return RedirectToPage("/Admin/StaffManager");
        }
    }
}