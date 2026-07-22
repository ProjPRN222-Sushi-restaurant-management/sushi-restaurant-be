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
            if (allStaff == null)
            {
                return;
            }

            var query = allStaff.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var keyword = SearchString.Trim();
                query = query.Where(s =>
                    (s.FullName != null && s.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Phone != null && s.Phone.Contains(keyword)));
            }

            var orderedStaff = query.OrderByDescending(s => s.StaffId).ToList();
            StaffList = PaginatedList<Staff>.Create(orderedStaff, PageNumber, PageSize);
        }

        public async Task<IActionResult> OnPostAddStaffAsync(string NewFullName, string NewPhone, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(NewFullName) ||
                string.IsNullOrWhiteSpace(NewPhone) ||
                string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin.";
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
            TempData[result ? "Success" : "Error"] = result
                ? "Thêm nhân viên thành công."
                : "Không thể thêm nhân viên. Số điện thoại có thể đã tồn tại.";

            return RedirectToPage("/Admin/StaffManager");
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần xóa.";
                return RedirectToPage("/Admin/StaffManager");
            }

            try
            {
                staff.IsActive = false;
                staff.DeletedAt = DateTime.Now;

                _context.Staffs.Update(staff);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa tài khoản nhân viên {staff.FullName} thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống khi xóa: " + ex.Message;
            }

            return RedirectToPage("/Admin/StaffManager");
        }
    }
}
