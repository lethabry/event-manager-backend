using EventManager.Common.DTOs;
namespace Booking.Application.Services.EventClientService;

public interface IEventClientService
{
    Task<EventResponseDTO?> GetEventByIdAsync(Guid id);
}
