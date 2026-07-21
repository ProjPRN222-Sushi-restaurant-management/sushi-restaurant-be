using DataAccessObjects;
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

static void ValidateIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value) ||
        value.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
    {
        throw new InvalidOperationException($"Invalid database identifier: {value}");
    }
}
