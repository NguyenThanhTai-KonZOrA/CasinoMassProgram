namespace Implement.EntityModels;

public class TeamRepresentativeMember
{
    public Guid TeamRepresentativeId { get; set; }
    public TeamRepresentative? TeamRepresentative { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}