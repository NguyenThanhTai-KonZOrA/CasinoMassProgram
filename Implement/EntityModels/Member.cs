namespace Implement.EntityModels;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // required, unique
    public string MemberCode { get; set; } = string.Empty; 
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Tier { get; set; }
    public int Points { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}