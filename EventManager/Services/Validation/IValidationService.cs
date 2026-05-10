using EventManager.Models;

namespace EventManager.Services.Validation;

public interface IValidationService
{
    public void ValidateEventDTO(EventDTO eventDTO);
}