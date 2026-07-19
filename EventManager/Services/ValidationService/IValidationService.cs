using EventManager.Models;

namespace EventManager.Services.ValidationService;

public interface IValidationService
{
    public void ValidateEventDTO(CreateEventDTO eventDTO);
    public void ValidateEventDTO(EventInfoDTO eventDTO);
    public void ValidatePaginatedResult(DateTime? from, DateTime? to, int page, int pageSize);
}
