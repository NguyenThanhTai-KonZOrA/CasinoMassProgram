namespace Implement.ViewModels.Request
{
    public class PaymentTeamRepresentativesRequest
    {
        public Guid PaymentTeamRepresentativesId { get; set; }
        public string TeamRepresentativeName { get; set; }
        public string TeamRepresentativeId { get; set; }
        public DateTime? Month { get; set; }
    }
}
