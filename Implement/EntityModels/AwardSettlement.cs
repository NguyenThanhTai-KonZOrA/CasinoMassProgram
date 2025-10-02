using System.ComponentModel.DataAnnotations;

namespace Implement.EntityModels;

public class AwardSettlement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // FK to TeamRepresentative (from Excel: ID + Team Representative + SEGMENT)
    public Guid TeamRepresentativeId { get; set; }
    public TeamRepresentative? TeamRepresentative { get; set; }

    // FK to Member (from Excel: Member ID + Member name)
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    // First day of the month, parsed from "Month"
    public DateOnly MonthStart { get; set; }

    [MaxLength(200)]
    public string SettlementDoc { get; set; } = string.Empty;

    public int No { get; set; }

    public DateOnly JoinedDate { get; set; }
    public DateOnly LastGamingDate { get; set; }
    public bool Eligible { get; set; }

    public decimal CasinoWinLoss { get; set; }
    public decimal AwardSettlementAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}