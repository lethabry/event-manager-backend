using System.ComponentModel.DataAnnotations;
namespace EventManager.Application.DTOs;

public record CreateEventDTO
{
    [Required(ErrorMessage = "Название мероприятия обязательно к заполнению")]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата и время начала мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "Дата и время окончания мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }

    [Required(ErrorMessage = "Количество мест обязательно к заполнению")]
    [Range(1, Int32.MaxValue, ErrorMessage = "Количество мест должно быть больше 0")]
    public int TotalSeats { get; set; }
}
