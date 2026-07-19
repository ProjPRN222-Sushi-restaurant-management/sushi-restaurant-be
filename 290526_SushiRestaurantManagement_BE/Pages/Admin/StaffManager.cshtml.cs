using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class StaffManagerModel : PageModel
    {
        private readonly IStaffService _staffService;
        private readonly RestaurantSystemDbContext _context;

        public StaffManagerModel(IStaffService staffService, RestaurantSystemDbContext context)
        {
            _staffService = staffService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public PaginatedList<Staff> StaffList { get; set; } = new(new List<Staff>(), 0, 1, 10);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var allStaff = await _staffService.GetAllStaffsAsync();
            if (allStaff != null)
            {
                var query = allStaff.AsQueryable();
                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(s => s.FullName.Contains(SearchString.Trim(), StringComparison.OrdinalIgnoreCase)
                                          || s.Phone.Contains(SearchString.Trim()));
                }
                var orderedStaff = query.OrderByDescending(s => s.StaffId).ToList();
                StaffList = PaginatedList<Staff>.Create(orderedStaff, PageNumber, PageSize);
            }
        }

        // HANDLER TH�M M?I STAFF
        public async Task<IActionResult> OnPostAddStaffAsync(string NewFullName, string NewPhone, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPhone) || string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "Vui l�ng ?i?n ??y ?? th�ng tin!";
                return RedirectToPage();
            }

            var staff = new Staff
            {
                FullName = NewFullName.Trim(),
                Phone = NewPhone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _staffService.AddStaffAsync(staff);
            if (result) TempData["Success"] = "Th�m nh�n vi�n th�nh c�ng!";
            else TempData["Error"] = "Kh�ng th? th�m nh�n vi�n (S? ?i?n tho?i c� th? ?� t?n t?i).";

            return RedirectToPage("/Admin/StaffManager");
        }

        // HANDLER X�A STAFF
        // Thay ??i tham s? id t? int sang long ?? kh?p ki?u d? li?u v?i Database
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // T�m ki?m staff v?i kh�a ch�nh ki?u long
            var staff = await _context.Staffs.FindAsync(id);

            if (staff != null)
            {
                try
                {
                    // TH?C HI?N X�A M?M (SOFT DELETE):
                    // C?p nh?t tr?ng th�i ho?t ??ng v? false v� l?u m?c th?i gian x�a
                    staff.IsActive = false;
                    staff.DeletedAt = DateTime.Now;

                    _context.Staffs.Update(staff);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"?� x�a m?m t�i kho?n nh�n vi�n {staff.FullName} th�nh c�ng!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "L?i h? th?ng khi x�a: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Kh�ng t�m th?y nh�n vi�n c?n x�a.";
            }

            return RedirectToPage("/Admin/StaffManager");
        }
    }
}