using System.Net;
using EventManager.Exceptions;
using EventManager.Models;

namespace EventManager.Services.Validation;

public class ValidationService : IValidationService
{
    public void ValidateEventDTO(EventDTO eventDTO)
    {
        if (string.IsNullOrWhiteSpace(eventDTO.Title))
        {
            throw new EventException(HttpStatusCode.BadRequest, "Название мероприятия не может быть пустым");
        }

        if (eventDTO.StartAt == DateTime.MinValue)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Дата начала мероприятия должна быть заполнена");
        }

        if (eventDTO.EndAt == DateTime.MinValue)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Дата конца мероприятия должна быть заполнена");
        }

        if (eventDTO.StartAt >= eventDTO.EndAt)
        {
            throw new EventException(
                HttpStatusCode.BadRequest,
                "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"
            );
        }
    }
}