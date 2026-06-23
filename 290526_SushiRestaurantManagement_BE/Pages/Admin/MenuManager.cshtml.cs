using _290526_SushiRestaurantManagement_BE.Helpers;
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
        public PaginatedList<MenuItem> DishList { get; set; } = new(new List<MenuItem>(), 0, 1, 15);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 15;

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

                var orderedDishes = query.OrderBy(d => d.MenuItemId).ToList();
                DishList = PaginatedList<MenuItem>.Create(orderedDishes, PageNumber, PageSize);
            }
        }

        public async Task<IActionResult> OnPostAddCategoryAsync(string NewCategoryName)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                TempData["Error"] = "T�n nh�m danh m?c kh�ng ???c ?? tr?ng.";
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
                    TempData["Success"] = "Th�m nh�m danh m?c m?i th�nh c�ng!";
                }
                else
                {
                    TempData["Error"] = "Th�m danh m?c th?t b?i. Vui l�ng ki?m tra l?i.";
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
                TempData["Error"] = "Th�ng tin t�n m�n ho?c gi� b�n nh?p v�o kh�ng h?p l?.";
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
                    TempData["Success"] = "Th�m m�n ?n m?i k�m h�nh ?nh v� m� t? th�nh c�ng!";
                }
                else
                {
                    TempData["Error"] = "Kh�ng th? th�m m�n ?n. Vui l�ng th? l?i.";
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
                    TempData["Success"] = "?� ng?ng b�n m�n ?n th�nh c�ng!";
                }
                else
                {
                    TempData["Error"] = "Kh�ng th? ng?ng b�n m�n ?n n�y.";
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

        //public async Task<IActionResult> OnPostDeleteCategoryAsync(long categoryId)
        //{
        //    try
        //    {
        //        var hasItems = await _itemService.HasMenuItemsByCategoryAsync(categoryId);

        //        if (hasItems)
        //        {
        //            TempData["Error"] = "Không thể xóa danh mục này vì vẫn còn món ăn thuộc danh mục.";
        //            return RedirectToPage("/Admin/MenuManager");
        //        }

        //        var result = await _categoryService.DeleteMenuCategoryAsync(categoryId);

        //        TempData[result ? "Success" : "Error"] =
        //            result ? "Xóa danh mục thành công!" : "Không thể xóa danh mục.";

        //        return RedirectToPage("/Admin/MenuManager");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = "Lỗi khi xóa danh mục: " + ex.Message;
        //        return RedirectToPage("/Admin/MenuManager");
        //    }
        //}
    }
}