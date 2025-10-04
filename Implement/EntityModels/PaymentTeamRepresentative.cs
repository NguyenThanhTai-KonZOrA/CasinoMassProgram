using Common.Constants;
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

        [MaxLength(50)]
        public string Status { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDelete { get; set; } = false;
        public string CreatedBy { get; set; } = CommonContants.SystemUser;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = CommonContants.SystemUser;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}