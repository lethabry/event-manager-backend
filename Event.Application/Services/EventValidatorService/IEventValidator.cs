using Event.Application.DTOs;
namespace Event.Application.Services.EventValidatorService;

public interface IEventValidatorService
{
    public void ValidateEventDTO(CreateEventDTO eventDTO);
    public void ValidateEventDTO(EventInfoDTO eventDTO);
    public void ValidatePaginatedResult(DateTime? from, DateTime? to, int page, int pageSize);
}
