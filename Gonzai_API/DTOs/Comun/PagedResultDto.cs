namespace Gonzai_API.DTOs.Comun;

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
}
