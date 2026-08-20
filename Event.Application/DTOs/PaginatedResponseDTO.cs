namespace Event.Application.DTOs;

public record PaginatedResponseDTO<T>
{
    public int TotalAmount { get; init; }
    public IReadOnlyList<T> Result { get; init; } = Array.Empty<T>();
    public int CurrentPage { get; init; }
    public int CurrentPageSize { get; init; }
}