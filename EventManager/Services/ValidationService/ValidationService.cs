using System.Net;
using EventManager.Exceptions;
using EventManager.Models;

namespace EventManager.Services.ValidationService;

public class ValidationService : IValidationService
{
    public void ValidateEventDTO(CreateEventDTO eventDTO)
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

        if (eventDTO.TotalSeats <= 0)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Количество мест должно быть больше 0");
        }
    }

    public void ValidateEventDTO(EventInfoDTO eventDTO)
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

        if (eventDTO.TotalSeats <= 0)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Количество мест должно быть больше 0");
        }

        if (eventDTO.AvailableSeats < 0)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Количество доступных мест должно быть не меньше 0");
        }

        if (eventDTO.AvailableSeats > eventDTO.TotalSeats)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Количество доступных мест не может быть больше мест всего");
        }
    }
}
