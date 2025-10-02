namespace Implement.EntityModels;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MemberCode { get; set; } = string.Empty; // required, unique
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Tier { get; set; }
    public int Points { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}