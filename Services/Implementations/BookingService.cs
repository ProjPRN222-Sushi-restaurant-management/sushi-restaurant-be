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

        RestaurantTable? table;

        if (request.TableId.HasValue)
        {
            // Nhân viên chủ động chọn bàn -> chỉ cần bàn còn trống (không trùng khung giờ)
            var allTables = await _tableRepository.GetAllTablesAsync(ct);
            table = allTables.FirstOrDefault(t => t.TableId == request.TableId.Value);

            if (table == null)
                throw new InvalidOperationException("Bàn được chọn không tồn tại.");

            if (!await IsTableFreeAsync(table.TableId, request, ct))
                throw new InvalidOperationException(
                    $"Bàn {table.TableNum} đã có người đặt trùng khung giờ này. Vui lòng chọn bàn khác.");
        }
        else
        {
            // Không chọn -> hệ thống tự gán bàn phù hợp theo khu vực + sức chứa
            table = await FindAvailableTableAsync(request, ct);
            if (table == null)
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
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 90,
            GuestCount = request.AdultCount + request.ChildCount,
            TableId = table.TableId,
            BookingStatus = BookingStatusEnum.PENDING,
            Note = BuildBookingNote(request),
            CreatedAt = DateTime.Now
        };

        await _bookingRepository.AddAsync(booking, ct);
        await _bookingRepository.SaveChangesAsync(ct);

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
        catch
        {
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

        var reqStart = ToMinutes(request.BookingTime);
        var reqEnd = reqStart + (request.DurationMinutes > 0 ? request.DurationMinutes : 90);

        var bookedTableIds = bookings
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Where(b => Overlaps(reqStart, reqEnd, b))
            .Select(b => b.TableId)
            .ToHashSet();

        var guestCount = request.AdultCount + request.ChildCount;

        return tables
            .Where(t =>
                t.TableType == request.TableType &&
                t.Capacity >= guestCount &&
                !bookedTableIds.Contains(t.TableId))
            .OrderBy(t => t.Capacity)
            .ToList();
    }

    private static bool Overlaps(int reqStart, int reqEnd, Booking existing)
    {
        var bStart = ToMinutes(existing.BookingTime);
        var bEnd = bStart + (existing.DurationMinutes > 0 ? existing.DurationMinutes : 90);
        return reqStart < bEnd && bStart < reqEnd;
    }

    private static int ToMinutes(TimeOnly t) => (int)t.ToTimeSpan().TotalMinutes;

    private async Task<bool> IsTableFreeAsync(long tableId, CreateBookingRequest request, CancellationToken ct)
    {
        var bookings = await _bookingRepository.GetByDateAsync(request.BookingDate, ct);
        var reqStart = ToMinutes(request.BookingTime);
        var reqEnd = reqStart + (request.DurationMinutes > 0 ? request.DurationMinutes : 90);
        return !bookings.Any(b =>
            b.TableId == tableId &&
            b.BookingStatus != BookingStatusEnum.CANCELLED &&
            Overlaps(reqStart, reqEnd, b));
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
        }
        else
        {
            customer.FullName = request.GuestName.Trim();
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
            throw new ArgumentException("Tên khách hàng không được để trống.", nameof(request.GuestName));

        if (string.IsNullOrWhiteSpace(request.GuestPhone))
            throw new ArgumentException("Số điện thoại không được để trống.", nameof(request.GuestPhone));

        if (request.AdultCount + request.ChildCount <= 0)
            throw new ArgumentException("Số lượng khách phải lớn hơn 0.", nameof(request.AdultCount));

        var now = DateTime.Now;
        var currentMinute = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        if (request.BookingDate.ToDateTime(request.BookingTime) < currentMinute)
            throw new InvalidOperationException(
                "Không thể đặt bàn cho thời điểm đã qua. Vui lòng chọn ngày/giờ từ hiện tại trở đi.");
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

        var temporaryTotal = orders.Sum(o =>
            o.TotalAmount > 0
                ? o.TotalAmount
                : o.OrderItems.Sum(d => d.Quantity * d.UnitPrice));

        return new BookingOrderHistoryResult
        {
            Booking = booking,
            Orders = orders,
            TemporaryTotal = temporaryTotal
        };
    }
}
