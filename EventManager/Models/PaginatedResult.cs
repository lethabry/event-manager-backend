namespace EventManager.Models;

public class PaginatedResultDTO<T>
{
    public int totalAmount { get; set; }
    public List<T> result { get; set; }
    public int currentPage { get; set; }
    public int currentPageSize { get; set; }
}