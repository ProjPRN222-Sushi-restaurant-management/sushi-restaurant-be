using DataAccessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<RestaurantSystemDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IMenuCategoryService, MenuCategoryService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<IRestaurantTableService, RestaurantTableService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddScoped<ITableAvailabilityService, TableAvailabilityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddHostedService<TableStatusUpdateHostedService>();

var app = builder.Build();

await EnsureDatabaseCompatibilityAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();

static async Task EnsureDatabaseCompatibilityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantSystemDbContext>();

    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE bookings MODIFY booking_status VARCHAR(20) NOT NULL DEFAULT 'PENDING';");

    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE orders MODIFY order_status VARCHAR(20) NOT NULL DEFAULT 'PENDING';");

    await EnsureColumnAsync(dbContext, "customers", "loyalty_points", "INT NOT NULL DEFAULT 0");
    await EnsureColumnAsync(dbContext, "customers", "membership_level", "VARCHAR(20) NOT NULL DEFAULT 'NONE'");

    await dbContext.Database.ExecuteSqlRawAsync(
        """
        UPDATE customers
        SET membership_level = CASE
            WHEN loyalty_points >= 1000 THEN 'DIAMOND'
            WHEN loyalty_points >= 500 THEN 'GOLD'
            WHEN loyalty_points >= 100 THEN 'SILVER'
            ELSE 'NONE'
        END;
        """);

    await EnsureColumnAsync(dbContext, "orders", "invoice_staff_id", "BIGINT NULL");
    await EnsureColumnAsync(dbContext, "orders", "invoice_staff_name", "VARCHAR(150) NULL");
    await EnsureColumnAsync(dbContext, "orders", "invoice_issued_at", "TIMESTAMP NULL");
    await EnsureColumnAsync(dbContext, "orders", "received_staff_id", "BIGINT NULL");
    await EnsureColumnAsync(dbContext, "orders", "received_staff_name", "VARCHAR(150) NULL");
    await EnsureColumnAsync(dbContext, "orders", "received_at", "TIMESTAMP NULL");
    await EnsureColumnAsync(dbContext, "orders", "subtotal_amount", "DECIMAL(12,2) NOT NULL DEFAULT 0");
    await EnsureColumnAsync(dbContext, "orders", "membership_level_applied", "VARCHAR(20) NOT NULL DEFAULT 'NONE'");
    await EnsureColumnAsync(dbContext, "orders", "discount_percent", "DECIMAL(5,2) NOT NULL DEFAULT 0");
    await EnsureColumnAsync(dbContext, "orders", "discount_amount", "DECIMAL(12,2) NOT NULL DEFAULT 0");
    await EnsureColumnAsync(dbContext, "orders", "earned_loyalty_points", "INT NOT NULL DEFAULT 0");

    await dbContext.Database.ExecuteSqlRawAsync(
        "UPDATE orders SET subtotal_amount = total_amount WHERE subtotal_amount = 0;");

    await dbContext.Database.ExecuteSqlRawAsync(
        "UPDATE orders SET earned_loyalty_points = FLOOR(total_amount / 10000) WHERE earned_loyalty_points = 0 AND order_status = 'COMPLETED';");

    await EnsureMenuSeedDataAsync(dbContext);
}

static async Task EnsureColumnAsync(
    RestaurantSystemDbContext dbContext,
    string tableName,
    string columnName,
    string columnDefinition)
{
    ValidateIdentifier(tableName);
    ValidateIdentifier(columnName);

    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State == ConnectionState.Closed;

    if (shouldClose)
    {
        await dbContext.Database.OpenConnectionAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        var columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@columnName";
        columnParameter.Value = columnName;
        command.Parameters.Add(columnParameter);

        var result = await command.ExecuteScalarAsync();
        var exists = Convert.ToInt32(result) > 0;

        if (!exists)
        {
            var alterSql = string.Concat(
                "ALTER TABLE `",
                tableName,
                "` ADD COLUMN `",
                columnName,
                "` ",
                columnDefinition,
                ";");

            await dbContext.Database.ExecuteSqlRawAsync(alterSql);
        }
    }
    finally
    {
        if (shouldClose)
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}

static async Task EnsureMenuSeedDataAsync(RestaurantSystemDbContext dbContext)
{
    await RenameOrMergeCategoryAsync(dbContext, "Món nóng", "Hot Kitchen");
    await RenameOrMergeCategoryAsync(dbContext, "Mì & Cơm", "Rice & Noodles");
    await RenameOrMergeCategoryAsync(dbContext, "Đồ uống", "Beverages");
    await RenameOrMergeCategoryAsync(dbContext, "Tráng miệng", "Desserts");
    await RenameOrMergeCategoryAsync(dbContext, "Dessert", "Desserts");
    await NormalizeMenuCategoriesAsync(dbContext);

    var seedItems = new[]
    {
        new MenuSeedItem("Sushi & Sashimi", "Dragon Roll", "Cơm cuộn lươn nướng, bơ, dưa leo và sốt kabayaki.", "images/dragon-roll.png", 169000m),
        new MenuSeedItem("Sushi & Sashimi", "Spicy Tuna Roll", "Cơm cuộn cá ngừ cay, mè rang và sốt mayo Nhật.", "images/spicy-tuna-roll.png", 149000m),
        new MenuSeedItem("Sushi & Sashimi", "Unagi Nigiri", "Nigiri lươn Nhật nướng sốt ngọt mặn, phủ mè rang.", "images/unagi-nigiri.png", 139000m),
        new MenuSeedItem("Sushi & Sashimi", "Hotate Sashimi", "Sò điệp Nhật cắt lát lạnh, vị ngọt thanh và mềm.", "images/hotate-sashimi.png", 189000m),
        new MenuSeedItem("Sushi & Sashimi", "Ikura Gunkan", "Gunkan trứng cá hồi tươi, rong biển giòn và cơm sushi.", "images/ikura-gunkan.png", 179000m),
        new MenuSeedItem("Sushi & Sashimi", "Tamago Nigiri", "Nigiri trứng cuộn Nhật mềm ngọt nhẹ, phù hợp trẻ em.", "images/tamago-nigiri.png", 69000m),
        new MenuSeedItem("Hot Kitchen", "Chicken Karaage", "Gà chiên kiểu Nhật, vỏ giòn, ăn kèm sốt mayo chanh.", "images/chicken-karaage.png", 109000m),
        new MenuSeedItem("Hot Kitchen", "Pork Gyoza", "Há cảo áp chảo nhân thịt heo và rau củ, ăn kèm sốt ponzu.", "images/pork-gyoza.png", 99000m),
        new MenuSeedItem("Hot Kitchen", "Takoyaki", "Bánh bạch tuộc Osaka phủ cá bào, rong biển và sốt takoyaki.", "images/takoyaki.png", 89000m),
        new MenuSeedItem("Hot Kitchen", "Salmon Teriyaki", "Cá hồi áp chảo sốt teriyaki, ăn kèm rau củ.", "images/salmon-teriyaki.png", 199000m),
        new MenuSeedItem("Hot Kitchen", "Beef Yakiniku", "Bò nướng sốt yakiniku đậm vị, dùng nóng.", "images/beef-yakiniku.png", 189000m),
        new MenuSeedItem("Hot Kitchen", "Miso Soup", "Súp miso nóng với đậu hũ, rong biển wakame và hành lá.", "images/miso-soup.png", 49000m),
        new MenuSeedItem("Rice & Noodles", "Shoyu Ramen", "Ramen nước tương Nhật thanh vị, trứng lòng đào và chashu.", "images/shoyu-ramen.png", 139000m),
        new MenuSeedItem("Rice & Noodles", "Miso Ramen", "Ramen miso béo thơm, bắp ngọt, rong biển và thịt chashu.", "images/miso-ramen.png", 149000m),
        new MenuSeedItem("Rice & Noodles", "Chicken Katsu Curry", "Cơm cà ri Nhật ăn kèm gà chiên xù giòn.", "images/chicken-katsu-curry.png", 159000m),
        new MenuSeedItem("Rice & Noodles", "Unagi Don", "Cơm lươn Nhật nướng sốt kabayaki, phủ mè và rong biển.", "images/unagi-don.png", 229000m),
        new MenuSeedItem("Rice & Noodles", "Chirashi Don", "Cơm sushi tô với sashimi tổng hợp và trứng cá hồi.", "images/chirashi-don.png", 239000m),
        new MenuSeedItem("Beverages", "Yuzu Soda", "Soda thanh mát vị yuzu Nhật, chua nhẹ và thơm citrus.", "images/yuzu-soda.png", 59000m),
        new MenuSeedItem("Beverages", "Genmaicha Tea", "Trà xanh gạo rang Nhật, hậu vị thơm ấm.", "images/genmaicha-tea.png", 49000m),
        new MenuSeedItem("Beverages", "Japanese Lemon Tea", "Trà chanh kiểu Nhật, cân bằng vị chua ngọt.", "images/japanese-lemon-tea.png", 55000m),
        new MenuSeedItem("Beverages", "Ramune", "Nước ngọt Nhật chai bi truyền thống, vị trái cây.", "images/ramune.png", 65000m),
        new MenuSeedItem("Desserts", "Dorayaki", "Bánh rán Nhật nhân đậu đỏ mềm ngọt.", "images/dorayaki.png", 69000m),
        new MenuSeedItem("Desserts", "Matcha Pudding", "Pudding matcha mịn, thơm trà xanh và béo nhẹ.", "images/matcha-pudding.png", 79000m),
        new MenuSeedItem("Desserts", "Taiyaki", "Bánh cá Nhật nhân custard hoặc đậu đỏ, dùng nóng.", "images/taiyaki.png", 75000m)
    };

    var categoryNames = seedItems
        .Select(item => item.CategoryName)
        .Distinct()
        .ToList();

    var categories = await dbContext.MenuCategories
        .Where(category => categoryNames.Contains(category.CategoryName))
        .ToListAsync();

    foreach (var categoryName in categoryNames)
    {
        var category = categories.FirstOrDefault(c => c.CategoryName == categoryName);
        if (category == null)
        {
            category = new MenuCategory
            {
                CategoryName = categoryName,
                DeletedAt = null
            };
            dbContext.MenuCategories.Add(category);
            categories.Add(category);
        }
        else
        {
            category.DeletedAt = null;
        }
    }

    await dbContext.SaveChangesAsync();
    await NormalizeMenuCategoriesAsync(dbContext);

    categories = await dbContext.MenuCategories
        .Where(category => categoryNames.Contains(category.CategoryName))
        .ToListAsync();

    foreach (var seedItem in seedItems)
    {
        var category = categories.First(c => c.CategoryName == seedItem.CategoryName);
        var menuItem = await dbContext.MenuItems
            .FirstOrDefaultAsync(item => item.ItemName == seedItem.ItemName);

        if (menuItem == null)
        {
            dbContext.MenuItems.Add(new MenuItem
            {
                CategoryId = category.CategoryId,
                ItemName = seedItem.ItemName,
                Description = seedItem.Description,
                ImageUrl = seedItem.ImageUrl,
                Price = seedItem.Price,
                IsAvailable = true,
                DeletedAt = null
            });
        }
        else
        {
            menuItem.CategoryId = category.CategoryId;
            menuItem.Description = seedItem.Description;
            menuItem.ImageUrl = seedItem.ImageUrl;
            menuItem.Price = seedItem.Price;
            menuItem.IsAvailable = true;
            menuItem.DeletedAt = null;
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task RenameOrMergeCategoryAsync(
    RestaurantSystemDbContext dbContext,
    string oldName,
    string newName)
{
    var oldCategory = await dbContext.MenuCategories
        .FirstOrDefaultAsync(category => category.CategoryName == oldName);
    if (oldCategory == null)
    {
        return;
    }

    var newCategory = await dbContext.MenuCategories
        .FirstOrDefaultAsync(category => category.CategoryName == newName);
    if (newCategory == null)
    {
        oldCategory.CategoryName = newName;
        oldCategory.DeletedAt = null;
        await dbContext.SaveChangesAsync();
        return;
    }

    var oldItems = await dbContext.MenuItems
        .Where(item => item.CategoryId == oldCategory.CategoryId)
        .ToListAsync();

    foreach (var item in oldItems)
    {
        item.CategoryId = newCategory.CategoryId;
    }

    newCategory.DeletedAt = null;
    oldCategory.DeletedAt = DateTime.Now;
    await dbContext.SaveChangesAsync();
}

static async Task NormalizeMenuCategoriesAsync(RestaurantSystemDbContext dbContext)
{
    var categories = await dbContext.MenuCategories.ToListAsync();
    var groupedCategories = categories
        .Select(category => new
        {
            Category = category,
            CanonicalName = GetCanonicalMenuCategoryName(category.CategoryName)
        })
        .Where(item => item.CanonicalName != null)
        .GroupBy(item => item.CanonicalName!, StringComparer.OrdinalIgnoreCase);

    foreach (var group in groupedCategories)
    {
        var canonicalName = group.Key;
        var candidates = group
            .Select(item => item.Category)
            .OrderBy(category => string.Equals(category.CategoryName.Trim(), canonicalName, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(category => category.DeletedAt == null ? 0 : 1)
            .ThenBy(category => category.CategoryId)
            .ToList();

        var targetCategory = candidates.First();
        targetCategory.CategoryName = canonicalName;
        targetCategory.DeletedAt = null;

        foreach (var duplicateCategory in candidates.Where(category => category.CategoryId != targetCategory.CategoryId))
        {
            var duplicateItems = await dbContext.MenuItems
                .Where(item => item.CategoryId == duplicateCategory.CategoryId)
                .ToListAsync();

            foreach (var item in duplicateItems)
            {
                item.CategoryId = targetCategory.CategoryId;
            }

            duplicateCategory.DeletedAt = DateTime.Now;
        }
    }

    await dbContext.SaveChangesAsync();
}

static string? GetCanonicalMenuCategoryName(string? categoryName)
{
    if (string.IsNullOrWhiteSpace(categoryName))
    {
        return null;
    }

    return categoryName.Trim().ToLowerInvariant() switch
    {
        "sushi & sashimi" or "sushi and sashimi" => "Sushi & Sashimi",
        "hot kitchen" or "mon nong" or "món nóng" => "Hot Kitchen",
        "rice & noodles" or "rice and noodles" or "mi & com" or "mì & cơm" => "Rice & Noodles",
        "beverage" or "beverages" or "do uong" or "đồ uống" => "Beverages",
        "dessert" or "desserts" or "trang mieng" or "tráng miệng" => "Desserts",
        _ => null
    };
}

static void ValidateIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value) ||
        value.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
    {
        throw new InvalidOperationException($"Invalid database identifier: {value}");
    }
}

record MenuSeedItem(
    string CategoryName,
    string ItemName,
    string Description,
    string ImageUrl,
    decimal Price);

