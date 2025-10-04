namespace Implement.EntityModels;

public class ImportCellError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RowId { get; set; }
    public ImportRow? Row { get; set; }
    public string Column { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}