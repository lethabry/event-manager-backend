using EventManager.Contracts.DTOs;
namespace Booking.Application.Services.UserClientService;

public interface IUserClientService
{
    Task<UserExistingStatus?> CheckIfUserExistAsync(Guid id);
}
