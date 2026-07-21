using Microsoft.Extensions.Configuration;
using Services.Interfaces;

namespace Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<bool> SendBookingConfirmationAsync(
        string phoneNumber,
        long bookingId,
        DateTime bookingDateTime,
        string guestName,
        CancellationToken ct = default)
    {
        try
        {
            var message = $"Xin chào {guestName}, đặt bàn của bạn tại nhà hàng Sushi đã được xác nhận. " +
                         $"Mã booking: #{bookingId}. " +
                         $"Thời gian: {bookingDateTime:dd/MM/yyyy HH:mm}. " +
                         $"Cảm ơn bạn!";

            return await SendZaloMessageAsync(phoneNumber, message, ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendBookingStatusChangeAsync(
        string phoneNumber,
        long bookingId,
        string status,
        string message,
        CancellationToken ct = default)
    {
        try
        {
            var notificationMessage = $"[Cập nhật đặt bàn] Mã booking #{bookingId} - Trạng thái: {status}. {message}";

            return await SendZaloMessageAsync(phoneNumber, notificationMessage, ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendBookingReminderAsync(
        string phoneNumber,
        long bookingId,
        DateTime bookingDateTime,
        string guestName,
        CancellationToken ct = default)
    {
        try
        {
            var message = $"Nhắc nhở: {guestName}, bạn có đặt bàn tại nhà hàng Sushi lúc {bookingDateTime:HH:mm} hôm nay. " +
                         $"Mã booking: #{bookingId}. Vui lòng đến đúng giờ.";

            return await SendZaloMessageAsync(phoneNumber, message, ct);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SendZaloMessageAsync(
        string phoneNumber,
        string message,
        CancellationToken ct = default)
    {
        try
        {
            var zaloApiUrl = _configuration["Zalo:ApiUrl"];
            var zaloAccessToken = _configuration["Zalo:AccessToken"];

            if (string.IsNullOrEmpty(zaloApiUrl) || string.IsNullOrEmpty(zaloAccessToken))
            {
                Console.WriteLine("========== MOCK ZALO MESSAGE ==========");
                Console.WriteLine($"To phone: {phoneNumber}");
                Console.WriteLine($"Message: {message}");
                Console.WriteLine($"Sent at: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine("=======================================");

                return true;
            }

            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                phone = phoneNumber,
                message = message,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {zaloAccessToken}");

            var response = await client.PostAsync($"{zaloApiUrl}/send", content, ct);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Log error but don't throw - notification failure shouldn't break booking process
            return false;
        }
    }
}
