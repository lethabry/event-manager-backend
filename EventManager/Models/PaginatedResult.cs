#nullable disable
namespace EventManager.Models;

public class PaginatedResultDTO<T>
{
    public int TotalAmount { get; set; }
    public List<T> Result { get; set; }
    public int CurrentPage { get; set; }
    public int CurrentPageSize { get; set; }
}