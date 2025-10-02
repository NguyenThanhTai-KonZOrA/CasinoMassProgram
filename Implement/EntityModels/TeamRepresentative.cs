using System.ComponentModel.DataAnnotations;

namespace Implement.EntityModels;

public class TeamRepresentative
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Excel "ID" column; unique key for a representative
    [MaxLength(100)]
    public string ExternalId { get; set; } = string.Empty;

    // Excel "Team Representative" column (display name)
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Excel "SEGMENT" column (optional attribute for the rep)
    [MaxLength(100)]
    public string? Segment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeamRepresentativeMember> Members { get; set; } = new List<TeamRepresentativeMember>();
}