using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Implement.EntityModels
{
    public class PaymentTeamRepresentative
    {
        [Key]
        public Guid Id { get; set; }

        public Guid TeamRepresentativeId { get; set; }
        public TeamRepresentative? TeamRepresentative { get; set; }

        public DateOnly MonthStart { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AwardTotal { get; set; }

        // Status: Inprocess | Void | Falied
        [MaxLength(50)]
        public string Status { get; set; } = "Inprocess";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}