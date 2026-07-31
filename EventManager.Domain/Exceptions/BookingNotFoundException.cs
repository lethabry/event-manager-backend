namespace EventManager.Domain.Exceptions;

public sealed class BookingNotFoundException : BookingException
{
    public Guid BookingId { get; }

    public BookingNotFoundException(Guid bookingId)
        : base($"Бронирование с id {bookingId} не найдено")
    {
        BookingId = bookingId;
    }
}
