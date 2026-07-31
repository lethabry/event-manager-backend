#nullable disable
namespace EventManager.Domain.Models;

public class PaginatedResultDTO<T>
{
    public int TotalAmount { get; set; }
    public IReadOnlyList<T> Result { get; set; }
    public int CurrentPage { get; set; }
    public int CurrentPageSize { get; set; }
}