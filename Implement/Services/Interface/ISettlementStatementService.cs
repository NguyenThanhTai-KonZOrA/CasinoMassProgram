using Implement.ViewModels.Request;
using Implement.ViewModels.Response;

namespace Implement.Services.Interface
{
    public interface ISettlementStatementService
    {
        Task<List<SettlementStatementResponse>> SettlementStatementSearch(SettlementStatementRequest settlementStatementRequest);

        Task<List<TeamRepresentativesResponse>> GetTeamRepresentatives(TeamRepresentativesRequest request);

        Task<PaymentTeamRepresentativesResponse> PaymentTeamRepresentatives(PaymentTeamRepresentativesRequest paymentTeam, string currentUserName);

        Task<UnPaidTeamRepresentativesResponse> UnPaidTeamRepresentatives(UnPaidTeamRepresentativesRequest unPaidTeam, string currentUserName);
    }
}
