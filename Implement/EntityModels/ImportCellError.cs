namespace Implement.EntityModels;

public class ImportCellError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RowId { get; set; }
    public ImportRow? Row { get; set; }

    public string Column { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}