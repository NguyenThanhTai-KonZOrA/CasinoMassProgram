namespace Implement.ViewModels.Request
{
    public class SettlementStatementRequest
    {
        public string TeamRepresentativeName { get; set; }
        public string TeamRepresentativeId { get; set; }
        public string ProgramName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}