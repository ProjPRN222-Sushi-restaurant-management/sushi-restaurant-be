using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObjects.Models;
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

        public List<MenuCategory> Categories { get; set; } = new List<MenuCategory>();
        public List<MenuItem> DishList { get; set; } = new List<MenuItem>();

        public async Task OnGetAsync(int? selectedCategoryId, string? searchString)
        {
            SelectedCategoryId = selectedCategoryId;
            SearchString = searchString;

            var allCategories = await _categoryService.GetAllMenuCategoriesAsync();
            if (allCategories != null)
            {
                Categories = allCategories.ToList();
            }

            var allMenuItems = await _itemService.GetAllMenuItemsAsync();
            if (allMenuItems != null)
            {
                var query = allMenuItems.AsQueryable();

                if (SelectedCategoryId.HasValue)
                {
                    query = query.Where(d => d.CategoryId == SelectedCategoryId.Value);
                }

                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(d => d.ItemName.Contains(SearchString.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                DishList = query.OrderBy(d => d.MenuItemId).ToList();
            }
        }

        public async Task<IActionResult> OnPostAddCategoryAsync(string NewCategoryName)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                TempData["Error"] = "Tên nhóm danh m?c không ???c ?? tr?ng.";
                return RedirectToPage("/Admin/MenuManager");
            }

            try
            {
                var newCat = new MenuCategory
                {
                    CategoryName = NewCategoryName.Trim()
                };

                var result = await _categoryService.AddMenuCategoryAsync(newCat);
                if (result)
                {
                    TempData["Success"] = "Thêm nhóm danh m?c m?i thành công!";
                }
                else
                {
                    TempData["Error"] = "Thêm danh m?c th?t b?i. Vui lòng ki?m tra l?i.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "L?i h? th?ng: " + ex.Message;
            }

            return RedirectToPage("/Admin/MenuManager");
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
                TempData["Error"] = "Thông tin tên món ho?c giá bán nh?p vào không h?p l?.";
                return RedirectToPage("/Admin/MenuManager", new { selectedCategoryId = SelectedCategoryId });
            }

            try
            {
                var newItem = new MenuItem
                {
                    ItemName = NewDishName.Trim(),
                    CategoryId = NewDishCategoryId,
                    Price = NewDishPrice,
                    Description = NewDishDescription?.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(NewDishImageUrl) ? "images/default_sushi.jpg" : NewDishImageUrl.Trim(),
                    IsAvailable = true
                };

                var result = await _itemService.AddMenuItemAsync(newItem);
                if (result)
                {
                    TempData["Success"] = "Thêm món ?n m?i kèm hình ?nh và mô t? thành công!";
                }
                else
                {
                    TempData["Error"] = "Không th? thêm món ?n. Vui lòng th? l?i.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "L?i h? th?ng: " + ex.Message;
            }

            return RedirectToPage("/Admin/MenuManager", new { selectedCategoryId = SelectedCategoryId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _itemService.DeleteMenuItemAsync(id);

                if (result)
                {
                    TempData["Success"] = "?ã ng?ng bán món ?n thành công!";
                }
                else
                {
                    TempData["Error"] = "Không th? ng?ng bán món ?n này.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "L?i h? th?ng: " + ex.Message;
            }

            return RedirectToPage("/Admin/MenuManager", new
            {
                selectedCategoryId = SelectedCategoryId,
                searchString = SearchString
            });
        }
    }
}