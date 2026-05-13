using EventManager.Models;

namespace EventManager.Services.ValidationService;

public interface IValidationService
{
    public void ValidateEventDTO(EventDTO eventDTO);
}