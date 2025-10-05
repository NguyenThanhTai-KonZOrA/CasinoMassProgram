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
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public byte[]? FileContent { get; set; }

    public ICollection<ImportRow> Rows { get; set; } = new List<ImportRow>();
}