namespace Implement.ViewModels.Response
{
    public class SettlementStatementResponse
    {
        public Guid SettlementId { get; set; }  // unique per settlement
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public DateTime JoinedDate { get; set; }
        public DateTime LastGamingDate { get; set; }
        public bool Eligible { get; set; }
        public decimal CasinoWinLoss { get; set; }
    }
}
