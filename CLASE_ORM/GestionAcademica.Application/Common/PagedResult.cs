namespace GestionAcademica.Application.Common;

/// <summary>Respuesta estándar de un listado paginado del lado del servidor.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public static class PaginationRules
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
}
