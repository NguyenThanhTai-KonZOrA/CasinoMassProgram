namespace Implement.ViewModels.Response
{
    public class TeamRepresentativesResponse
    {
        public string Segment { get; set; }
        public string TeamRepresentativeName { get; set; }
        public string TeamRepresentativeId { get; set; }
        public Guid PaymentTeamRepresentativesId { get; set; }
        public string SettlementDoc { get; set; }
        public string ProgramName { get; set; }
        public DateTime Month { get; set; }
        public decimal AwardTotal { get; set; }
        public decimal CasinoWinLoss { get; set; }
        public string Status { get; set; } = string.Empty; // Inprocess | Void | Falied | 
        public bool IsPayment { get; set; }
    }
}
