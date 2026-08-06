namespace EventManager.Domain.Exceptions;

public class BookingStatusConflictException : BookingException
{
    public Guid BookingId { get; }

    public BookingStatusConflictException(Guid bookingId) : base($"Нельзя отменить бронь с id = {bookingId}")
    {
        BookingId = bookingId;
    }
}
