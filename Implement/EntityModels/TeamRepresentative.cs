using System.ComponentModel.DataAnnotations;

namespace Implement.EntityModels;

public class TeamRepresentative
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string TeamRepresentativeId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string TeamRepresentativeName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Segment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeamRepresentativeMember> Members { get; set; } = new List<TeamRepresentativeMember>();
}