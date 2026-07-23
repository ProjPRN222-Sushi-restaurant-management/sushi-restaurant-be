namespace _290526_SushiRestaurantManagement_BE.Utilities;

public static class MenuImageResolver
{
    private static readonly Dictionary<string, string> ImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Salmon Sushi"] = "salmon-sushi.png",
        ["Tuna Sushi"] = "tuna-sushi.png",
        ["Salmon Sashimi"] = "salmon-sashimi.png",
        ["Tuna Sashimi"] = "tuna-sashimi.png",
        ["Ebi Nigiri"] = "ebi-nigiri.png",
        ["California Roll"] = "California-roll.png",
        ["Tonkotsu Ramen"] = "tonkotsu-ramen.png",
        ["Tempura Udon"] = "tempura-udon.png",
        ["Shrimp Tempura"] = "shrimp-tempura.png",
        ["Matcha Latte"] = "match-latte.png",
        ["Sakura Tea"] = "sakura-tea.png",
        ["Mochi Ice Cream"] = "mochi-ice-cream.png",
        ["Dragon Roll"] = "dragon-roll.png",
        ["Spicy Tuna Roll"] = "spicy-tuna-roll.png",
        ["Unagi Nigiri"] = "unagi-nigiri.png",
        ["Hotate Sashimi"] = "hotate-sashimi.png",
        ["Ikura Gunkan"] = "ikura-gunkan.png",
        ["Tamago Nigiri"] = "tamago-nigiri.png",
        ["Chicken Karaage"] = "chicken-karaage.png",
        ["Pork Gyoza"] = "pork-gyoza.png",
        ["Takoyaki"] = "takoyaki.png",
        ["Salmon Teriyaki"] = "salmon-teriyaki.png",
        ["Beef Yakiniku"] = "beef-yakiniku.png",
        ["Miso Soup"] = "miso-soup.png",
        ["Shoyu Ramen"] = "shoyu-ramen.png",
        ["Miso Ramen"] = "miso-ramen.png",
        ["Chicken Katsu Curry"] = "chicken-katsu-curry.png",
        ["Unagi Don"] = "unagi-don.png",
        ["Chirashi Don"] = "chirashi-don.png",
        ["Yuzu Soda"] = "yuzu-soda.png",
        ["Genmaicha Tea"] = "genmaicha-tea.png",
        ["Japanese Lemon Tea"] = "japanese-lemon-tea.png",
        ["Ramune"] = "ramune.png",
        ["Dorayaki"] = "dorayaki.png",
        ["Matcha Pudding"] = "matcha-pudding.png",
        ["Taiyaki"] = "taiyaki.png",
    };

    public static string ResolveImageUrl(string? itemName, string? imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(itemName) &&
            ImageMap.TryGetValue(itemName.Trim(), out var file))
        {
            return "/images/" + file;
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return "/images/placeholder.svg";
        }

        if (imageUrl.StartsWith("http://") ||
            imageUrl.StartsWith("https://") ||
            imageUrl.StartsWith("/"))
        {
            return imageUrl;
        }

        return "/" + imageUrl.TrimStart('~', '/');
    }
}
