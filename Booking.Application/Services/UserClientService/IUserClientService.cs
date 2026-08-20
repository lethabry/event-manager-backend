using EventManager.Common.DTOs;
namespace Booking.Application.Services.UserClientService;

public interface IUserClientService
{
    Task<UserExistingStatus?> CheckIfUserExistAsync(Guid id);
}
