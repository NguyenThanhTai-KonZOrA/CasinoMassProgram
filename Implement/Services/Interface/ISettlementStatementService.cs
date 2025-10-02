using Implement.ViewModels.Request;
using Implement.ViewModels.Response;

namespace Implement.Services.Interface
{
    public interface ISettlementStatementService
    {
        Task<List<SettlementStatementResponse>> SettlementStatementSearch(SettlementStatementRequest settlementStatementRequest);
    }
}
