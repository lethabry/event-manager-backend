using Event.Application.DTOs;
using Event.Domain.Exceptions;
namespace Event.Application.Services.EventValidatorService;

public class EventValidatorService : IEventValidatorService
{
    public void ValidateEventDTO(CreateEventDTO eventDTO)
    {
        if (string.IsNullOrWhiteSpace(eventDTO.Title))
        {
            throw new EventValidationException("Название мероприятия не может быть пустым");
        }

        if (eventDTO.StartAt == DateTime.MinValue)
        {
            throw new EventValidationException("Дата начала мероприятия должна быть заполнена");
        }

        if (eventDTO.EndAt == DateTime.MinValue)
        {
            throw new EventValidationException("Дата конца мероприятия должна быть заполнена");
        }

        if (eventDTO.StartAt >= eventDTO.EndAt)
        {
            throw new EventValidationException(
                "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"
            );
        }

        if (eventDTO.TotalSeats <= 0)
        {
            throw new EventValidationException("Количество мест должно быть больше 0");
        }
    }

    public void ValidateEventDTO(EventInfoDTO eventDTO)
    {
        if (string.IsNullOrWhiteSpace(eventDTO.Title))
        {
            throw new EventValidationException("Название мероприятия не может быть пустым");
        }

        if (eventDTO.StartAt == DateTime.MinValue)
        {
            throw new EventValidationException("Дата начала мероприятия должна быть заполнена");
        }

        if (eventDTO.EndAt == DateTime.MinValue)
        {
            throw new EventValidationException("Дата конца мероприятия должна быть заполнена");
        }

        if (eventDTO.StartAt >= eventDTO.EndAt)
        {
            throw new EventValidationException(
                "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"
            );
        }

        if (eventDTO.TotalSeats <= 0)
        {
            throw new EventValidationException("Количество мест должно быть больше 0");
        }

        if (eventDTO.AvailableSeats < 0)
        {
            throw new EventValidationException("Количество доступных мест должно быть не меньше 0");
        }

        if (eventDTO.AvailableSeats > eventDTO.TotalSeats)
        {
            throw new EventValidationException("Количество доступных мест не может быть больше мест всего");
        }
    }

    public void ValidatePaginatedResult(DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
        {
            throw new EventValidationException(
                "Дата начала мероприятия должны быть раньше даты окончания мероприятия");
        }

        if (page <= 0)
        {
            throw new EventValidationException("Номер страницы не может быть меньше 1");
        }

        if (pageSize <= 0)
        {
            throw new EventValidationException("Количество элементов не может быть меньше 1");
        }

        if ((long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new EventValidationException("Смещение страницы слишком велико");
        }
    }
}
