using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;

namespace SeatSync.Core.Services;

public class BookingService : IBookingService
{
    public const string ConfirmQueueName = "booking.confirm";

    private readonly IEventPublisher _eventPublisher;
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IReceiptGenerator _receiptGenerator;

    public BookingService(
        IEventPublisher eventPublisher,
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ISeatRepository seatRepository,
        IUserRepository userRepository,
        IReceiptGenerator receiptGenerator)
    {
        _eventPublisher = eventPublisher;
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _seatRepository = seatRepository;
        _userRepository = userRepository;
        _receiptGenerator = receiptGenerator;
    }

    public async Task<ConfirmBookingAcceptedResponse> RequestConfirmationAsync(Guid userId, ConfirmBookingRequest request)
    {
        var bookingRequestId = Guid.NewGuid();
        var message = new ConfirmBookingMessage(bookingRequestId, userId, request.EventId, request.SeatIds);
        await _eventPublisher.PublishAsync(ConfirmQueueName, message);
        return new ConfirmBookingAcceptedResponse(bookingRequestId, "Pending");
    }

    public async Task<List<BookingResponse>> GetMyBookingsAsync(Guid userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        return bookings.Select(ToResponse).ToList();
    }

    public async Task<BookingResponse?> GetByRequestIdAsync(Guid userId, Guid bookingRequestId)
    {
        var booking = await _bookingRepository.GetByRequestIdAsync(bookingRequestId);
        if (booking is null || booking.UserId != userId) return null;
        return ToResponse(booking);
    }

    public async Task<byte[]?> GenerateReceiptAsync(Guid userId, Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null || booking.UserId != userId || booking.Status != BookingStatus.Confirmed)
            return null;

        var @event = await _eventRepository.GetByIdAsync(booking.EventId);
        var user = await _userRepository.GetByIdAsync(userId);
        var seats = await _seatRepository.GetByIdsAsync(booking.SeatIds);

        if (@event is null || user is null) return null;

        var receiptData = new ReceiptData(
            @event.Name,
            @event.Venue,
            @event.DateTime,
            user.DisplayName,
            seats.Select(s => new ReceiptSeatLine($"{s.Row}{s.Number}", s.Price)).ToList(),
            seats.Sum(s => s.Price)
        );

        return _receiptGenerator.GenerateBookingReceipt(receiptData);
    }

    private static BookingResponse ToResponse(Booking b)
        => new(b.Id, b.EventId, b.SeatIds, b.Status.ToString(), b.CreatedAt, b.ConfirmedAt);
}