namespace EventManager.Application.DTOs;

public record EventResponseDTO
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
}