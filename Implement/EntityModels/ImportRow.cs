using System.Text.Json;

namespace Implement.EntityModels;

public class ImportRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public ImportBatch? Batch { get; set; }
    // Excel row index (1-based)
    public int RowNumber { get; set; } 
    public bool IsValid { get; set; }

    public string RawJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<ImportCellError> Errors { get; set; } = new List<ImportCellError>();

    public Dictionary<string, string> RawAsDictionary() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(RawJson) ?? new();
}