using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects.Requests;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly INotificationService _notificationService;
    private readonly IOrderRepository _orderRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        ICustomerRepository customerRepository,
        IRestaurantTableRepository tableRepository,
        INotificationService notificationService,
        IOrderRepository orderRepository)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
        _tableRepository = tableRepository;
        _notificationService = notificationService;
        _orderRepository = orderRepository;
    }

    public Task<Booking> GetBookingByIdAsync(long id, CancellationToken ct = default)
        => _bookingRepository.GetBookingByIdAsync((int)id, ct);

    public Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        => _bookingRepository.GetAllBookingsAsync(ct);

    public Task<IEnumerable<Booking>> GetByDateAsync(DateOnly bookingDate, CancellationToken ct = default)
        => _bookingRepository.GetByDateAsync(bookingDate, ct);

    public Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatusEnum status, CancellationToken ct = default)
        => _bookingRepository.GetByStatusAsync(status, ct);

    public async Task<Booking> CreateBookingAsync(
    CreateBookingRequest request,
    CancellationToken ct = default)
    {
        ValidateCreateBookingRequest(request);

        var table = await FindAvailableTableAsync(request, ct);
        if (table == null)
        {
            // Thay vì throw Exception chung chung, hãy dùng một Exception có định danh
            throw new InvalidOperationException("Không còn bàn trống phù hợp trong khung giờ này.");
        }

        var customer = await CreateOrUpdateCustomerAsync(request, ct);

        var booking = new Booking
        {
            CustomerId = customer.CustomerId,
            GuestName = request.GuestName.Trim(),
            GuestPhone = request.GuestPhone.Trim(),
            BookingDate = request.BookingDate,
            BookingTime = request.BookingTime,
            GuestCount = request.AdultCount + request.ChildCount,
            TableId = table.TableId,
            BookingStatus = BookingStatusEnum.PENDING,
            Note = BuildBookingNote(request),
            CreatedAt = DateTime.Now
        };

        await _bookingRepository.AddAsync(booking, ct);
        await _bookingRepository.SaveChangesAsync(ct);

        // Gửi thông báo an toàn (Không để lỗi gửi tin nhắn làm ảnh hưởng đến kết quả đặt bàn)
        try
        {
            var bookingDateTime = booking.BookingDate.ToDateTime(booking.BookingTime);
            await _notificationService.SendBookingConfirmationAsync(
                booking.GuestPhone,
                booking.BookingId,
                bookingDateTime,
                booking.GuestName
            );
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Gửi tin nhắn xác nhận thất bại cho Booking #{BookingId}", booking.BookingId);
        }

        return booking;
    }

    public async Task<List<RestaurantTable>> GetAvailableTablesAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        var tables = await _tableRepository.GetAllTablesAsync(ct);

        var bookings = await _bookingRepository.GetByDateAsync(
            request.BookingDate, ct);

        var bookedTableIds = bookings
            .Where(b =>
                b.BookingTime == request.BookingTime &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Select(b => b.TableId)
            .ToList();

        var guestCount = request.AdultCount + request.ChildCount;

        return tables
            .Where(t =>
                t.TableType == request.TableType &&
                t.Capacity >= guestCount &&
                !bookedTableIds.Contains(t.TableId))
            .OrderBy(t => t.Capacity)
            .ToList();
    }

    public async Task UpdateBookingStatusAsync(
        long bookingId,
        BookingStatusEnum status,
        CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync((int)bookingId, ct);

        if (booking == null)
        {
            throw new Exception("Không tìm thấy booking.");
        }

        booking.BookingStatus = status;

        await _bookingRepository.UpdateAsync(booking, ct);
        await _bookingRepository.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
        => _bookingRepository.UpdateAsync(booking, ct);

    public Task DeleteAsync(long bookingId, CancellationToken ct = default)
        => _bookingRepository.DeleteAsync(bookingId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _bookingRepository.SaveChangesAsync(ct);

    public Task<bool> ExistsAsync(long bookingId, CancellationToken ct = default)
        => _bookingRepository.ExistsAsync(bookingId, ct);

    private async Task<RestaurantTable?> FindAvailableTableAsync(
        CreateBookingRequest request,
        CancellationToken ct)
    {
        var availableTables = await GetAvailableTablesAsync(request, ct);
        return availableTables.FirstOrDefault();
    }

    private async Task<Customer> CreateOrUpdateCustomerAsync(
        CreateBookingRequest request,
        CancellationToken ct)
    {
        var phone = request.GuestPhone.Trim();

        var customer = await _customerRepository.GetCustomerByPhoneAsync(phone, ct);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.GuestName.Trim(),
                Phone = phone,
                CreatedAt = DateTime.Now
            };

            await _customerRepository.AddCustomerAsync(customer, ct);
            await _bookingRepository.SaveChangesAsync(ct);
        }
        else
        {
            customer.FullName = request.GuestName.Trim();
            await _customerRepository.AddCustomerAsync(customer, ct);
            await _bookingRepository.SaveChangesAsync(ct);
        }

        return customer;
    }

    private static string BuildBookingNote(CreateBookingRequest request)
    {
        return $"Người lớn: {request.AdultCount}, Trẻ em: {request.ChildCount}. {request.Note}";
    }

    private static void ValidateCreateBookingRequest(CreateBookingRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.GuestName))
            throw new Exception("Tên khách hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(request.GuestPhone))
            throw new Exception("Số điện thoại không được để trống.");

        if (request.AdultCount + request.ChildCount <= 0)
            throw new Exception("Số lượng khách phải lớn hơn 0.");
    }

    public async Task<BookingOrderHistoryResult> GetBookingOrderHistoryAsync(
    long bookingId,
    CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync((int)bookingId, ct);

        if (booking == null)
        {
            throw new Exception("Không tìm thấy booking.");
        }

        var orders = await _orderRepository.GetOrdersByBookingIdAsync(bookingId);

        var temporaryTotal = orders
            .SelectMany(o => o.OrderItems)
            .Sum(d => d.Quantity * d.UnitPrice);

        return new BookingOrderHistoryResult
        {
            Booking = booking,
            Orders = orders,
            TemporaryTotal = temporaryTotal
        };
    }
}