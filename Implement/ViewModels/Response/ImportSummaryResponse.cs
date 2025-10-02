namespace Implement.ViewModels.Response;

public class ImportSummaryResponse
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = "";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public List<RowErrorDto> SampleErrors { get; set; } = new();
}

public class RowErrorDto
{
    public int RowNumber { get; set; }
    public List<CellErrorData> Errors { get; set; } = new();
}

public class CellErrorData
{
    public string Column { get; set; } = "";
    public string Message { get; set; } = "";
}