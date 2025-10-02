using System.Text.Json;

namespace Implement.EntityModels;

public class ImportRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public ImportBatch? Batch { get; set; }

    public int RowNumber { get; set; } // Excel row index (1-based)
    public bool IsValid { get; set; }

    // Store raw row as JSON (header:value)
    public string RawJson { get; set; } = "{}";

    public ICollection<ImportCellError> Errors { get; set; } = new List<ImportCellError>();

    public Dictionary<string, string> RawAsDictionary() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(RawJson) ?? new();
}