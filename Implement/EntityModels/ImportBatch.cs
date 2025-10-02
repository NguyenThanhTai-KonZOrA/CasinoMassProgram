namespace Implement.EntityModels;

public class ImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Validated"; // Validated | Committed
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }

    // Store original file to reproduce annotated download
    public byte[]? FileContent { get; set; }

    public ICollection<ImportRow> Rows { get; set; } = new List<ImportRow>();
}