using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Implement.EntityModels
{
    public class PaymentTeamRepresentative
    {
        [Key]
        public Guid Id { get; set; }

        // FK tới TeamRepresentative
        public Guid TeamRepresentativeId { get; set; }
        public TeamRepresentative? TeamRepresentative { get; set; }

        // Tháng áp dụng (ngày đầu tháng)
        public DateOnly MonthStart { get; set; }

        // Tổng tiền thanh toán theo tháng
        [Column(TypeName = "decimal(18,2)")]
        public decimal AwardTotal { get; set; }

        // Trạng thái: Inprocess | Void | Falied
        [MaxLength(50)]
        public string Status { get; set; } = "Inprocess";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}