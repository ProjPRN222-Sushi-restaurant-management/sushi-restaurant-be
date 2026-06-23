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

        public List<Staff> StaffList { get; set; } = new();

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
                StaffList = query.OrderByDescending(s => s.StaffId).ToList();
            }
        }

        // HANDLER THÊM M?I STAFF
        public async Task<IActionResult> OnPostAddStaffAsync(string NewFullName, string NewPhone, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPhone) || string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "Vui lòng ?i?n ??y ?? thông tin!";
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
            if (result) TempData["Success"] = "Thêm nhân viên thành công!";
            else TempData["Error"] = "Không th? thêm nhân viên (S? ?i?n tho?i có th? ?ã t?n t?i).";

            return RedirectToPage("/Admin/StaffManager");
        }

        // HANDLER XÓA STAFF
        // Thay ??i tham s? id t? int sang long ?? kh?p ki?u d? li?u v?i Database
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            // Tìm ki?m staff v?i khóa chính ki?u long
            var staff = await _context.Staffs.FindAsync(id);

            if (staff != null)
            {
                try
                {
                    // TH?C HI?N XÓA M?M (SOFT DELETE):
                    // C?p nh?t tr?ng thái ho?t ??ng v? false và l?u m?c th?i gian xóa
                    staff.IsActive = false;
                    staff.DeletedAt = DateTime.Now;

                    _context.Staffs.Update(staff);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"?ã xóa m?m tài kho?n nhân viên {staff.FullName} thành công!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "L?i h? th?ng khi xóa: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Không tìm th?y nhân viên c?n xóa.";
            }

            return RedirectToPage("/Admin/StaffManager");
        }
    }
}