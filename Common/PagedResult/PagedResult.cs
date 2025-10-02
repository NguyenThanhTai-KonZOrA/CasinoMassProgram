namespace Common.PagedResult
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
