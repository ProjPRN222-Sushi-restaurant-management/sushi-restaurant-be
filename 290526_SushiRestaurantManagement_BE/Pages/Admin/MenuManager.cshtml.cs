using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class MenuManagerModel : PageModel
    {
        private readonly IMenuItemService _itemService;
        private readonly IMenuCategoryService _categoryService;

        public MenuManagerModel(IMenuItemService itemService, IMenuCategoryService categoryService)
        {
            _itemService = itemService;
            _categoryService = categoryService;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public List<MenuCategory> Categories { get; set; } = new();

        public PaginatedList<MenuItem> DishList { get; set; } = new(new List<MenuItem>(), 0, 1, 15);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 15;

        public async Task OnGetAsync(int? selectedCategoryId, string? searchString)
        {
            SelectedCategoryId = selectedCategoryId;
            SearchString = searchString;

            var allCategories = await _categoryService.GetAllMenuCategoriesAsync();
            Categories = allCategories?.ToList() ?? new List<MenuCategory>();

            var allMenuItems = await _itemService.GetAllMenuItemsAsync();
            if (allMenuItems == null)
            {
                return;
            }

            var query = allMenuItems.AsQueryable();
            if (SelectedCategoryId.HasValue)
            {
                query = query.Where(d => d.CategoryId == SelectedCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                query = query.Where(d =>
                    d.ItemName.Contains(SearchString.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            var orderedDishes = query.OrderBy(d => d.MenuItemId).ToList();
            DishList = PaginatedList<MenuItem>.Create(orderedDishes, PageNumber, PageSize);
        }

        public async Task<IActionResult> OnPostAddCategoryAsync(string NewCategoryName)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                TempData["Error"] = "Tên nhóm danh mục không được để trống.";
                return RedirectToMenuManager();
            }

            try
            {
                var newCategory = new MenuCategory
                {
                    CategoryName = NewCategoryName.Trim()
                };

                var result = await _categoryService.AddMenuCategoryAsync(newCategory);
                TempData[result ? "Success" : "Error"] = result
                    ? "Thêm nhóm danh mục mới thành công."
                    : "Thêm danh mục thất bại. Vui lòng kiểm tra lại.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToMenuManager();
        }

        public async Task<IActionResult> OnPostAddDishAsync(
            string NewDishName,
            int NewDishCategoryId,
            decimal NewDishPrice,
            string? NewDishImageUrl,
            string? NewDishDescription)
        {
            if (string.IsNullOrWhiteSpace(NewDishName) || NewDishPrice < 0)
            {
                TempData["Error"] = "Tên món hoặc giá bán không hợp lệ.";
                return RedirectToMenuManager();
            }

            try
            {
                var newItem = new MenuItem
                {
                    ItemName = NewDishName.Trim(),
                    CategoryId = NewDishCategoryId,
                    Price = NewDishPrice,
                    Description = NewDishDescription?.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(NewDishImageUrl)
                        ? "images/default_sushi.jpg"
                        : NewDishImageUrl.Trim(),
                    IsAvailable = true
                };

                var result = await _itemService.AddMenuItemAsync(newItem);
                TempData[result ? "Success" : "Error"] = result
                    ? "Thêm món ăn mới thành công."
                    : "Không thể thêm món ăn. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToMenuManager();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _itemService.DeleteMenuItemAsync(id);
                TempData[result ? "Success" : "Error"] = result
                    ? "Đã ngừng bán món ăn thành công."
                    : "Không thể ngừng bán món ăn này.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToMenuManager();
        }

        public async Task<IActionResult> OnPostDeleteCategoryAsync(long categoryId)
        {
            try
            {
                var hasMenuItems = await _itemService.HasMenuItemsByCategoryAsync(categoryId);
                if (hasMenuItems)
                {
                    TempData["Error"] = "Không thể xóa danh mục đang có món ăn.";
                    return RedirectToMenuManagerAfterCategoryDelete(categoryId);
                }

                var result = await _categoryService.DeleteMenuCategoryAsync(categoryId);
                TempData[result ? "Success" : "Error"] = result
                    ? "Đã xóa danh mục thành công."
                    : "Không thể xóa danh mục này.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToMenuManagerAfterCategoryDelete(categoryId);
        }

        private IActionResult RedirectToMenuManager() =>
            RedirectToPage("/Admin/MenuManager", new
            {
                selectedCategoryId = SelectedCategoryId,
                searchString = SearchString,
                PageNumber
            });

        private IActionResult RedirectToMenuManagerAfterCategoryDelete(long deletedCategoryId)
        {
            var selectedCategoryId = SelectedCategoryId == deletedCategoryId
                ? null
                : SelectedCategoryId;

            return RedirectToPage("/Admin/MenuManager", new
            {
                selectedCategoryId,
                searchString = SearchString,
                PageNumber
            });
        }
    }
}
