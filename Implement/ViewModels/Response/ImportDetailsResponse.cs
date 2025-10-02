namespace Implement.ViewModels.Response
{
    public class RowDetailsData
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, string> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CellErrorData> Errors { get; set; } = new();
    }

    public class ImportDetailsResponse
    {
        public Guid BatchId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<RowDetailsData> Rows { get; set; } = new();

        // Paging
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}