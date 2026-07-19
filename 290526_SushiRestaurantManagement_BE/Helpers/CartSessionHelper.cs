using System.Text.Json;

namespace _290526_SushiRestaurantManagement_BE.Helpers
{
    public static class CartSessionHelper
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }

    public class CartItemViewModel
    {
        public long MenuItemId { get; set; }

        public string ItemName { get; set; } = "";

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        public decimal Total => UnitPrice * Quantity;
    }
}
