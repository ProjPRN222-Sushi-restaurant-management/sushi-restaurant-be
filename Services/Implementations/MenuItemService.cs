using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class MenuItemService : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IOrderRepository _orderRepository;

    public MenuItemService(IMenuItemRepository menuItemRepository, IOrderRepository oderRepository)
    {
        _menuItemRepository = menuItemRepository;
        _orderRepository = oderRepository;
    }

    public Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default)
        => _menuItemRepository.GetAllMenuItemsAsync(ct);

    public Task<MenuItem> GetMenuItemByIdAsync(int id, CancellationToken ct = default)
        => _menuItemRepository.GetMenuItemByIdAsync(id, ct);

    public async Task<bool> AddMenuItemAsync(MenuItem item, CancellationToken ct = default)
    {
        // 1. Ki?m tra tính h?p l? d? li?u (Tên món tr?ng ho?c giá nh? h?n 0 thì t? ch?i)
        if (item == null || string.IsNullOrWhiteSpace(item.ItemName) || item.Price < 0)
        {
            return false;
        }

        // 2. Chu?n hóa chu?i v?n b?n g?n gàng
        item.ItemName = item.ItemName.Trim();

        try
        {
            // 3. G?i xu?ng t?ng Repository ?? thêm món ?n vào DB
            await _menuItemRepository.AddMenuItemAsync(item, ct);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteMenuItemAsync(int id, CancellationToken ct = default)
    {
        var menuItem = await _menuItemRepository.GetMenuItemByIdAsync(id, ct);

        if (menuItem == null)
        {
            throw new Exception("Món ?n không t?n t?i ho?c ?ã b? xóa tr??c ?ó.");
        }

        menuItem.IsAvailable = false;
        menuItem.DeletedAt = DateTime.Now;

        return await _menuItemRepository.UpdateMenuItemAsync(menuItem, ct);
    }
}
