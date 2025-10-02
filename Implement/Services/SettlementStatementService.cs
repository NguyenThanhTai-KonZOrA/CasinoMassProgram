using Implement.Repositories.Interface;
using Implement.Services.Interface;
using Implement.ViewModels.Request;
using Implement.ViewModels.Response;

namespace Implement.Services
{
    public class SettlementStatementService : ISettlementStatementService
    {
        private readonly IAwardSettlementRepository _awardSettlementRepository;

        public SettlementStatementService(IAwardSettlementRepository awardSettlementRepository)
        {
            _awardSettlementRepository = awardSettlementRepository;
        }

        public async Task<List<SettlementStatementResponse>> SettlementStatementSearch(SettlementStatementRequest settlementStatementRequest)
        {
            if (settlementStatementRequest == null) throw new ArgumentNullException(nameof(settlementStatementRequest));

            // Normalize dates (swap if EndDate < StartDate)
            var start = DateOnly.MaxValue;
            var end = DateOnly.MaxValue;
            if (settlementStatementRequest.StartDate.HasValue && settlementStatementRequest.EndDate.HasValue)
            {
                start = DateOnly.FromDateTime(settlementStatementRequest.StartDate.Value);
                end = DateOnly.FromDateTime(settlementStatementRequest.EndDate.Value);
                if (end < start) (start, end) = (end, start);
            }

            var trId = settlementStatementRequest.TeamRepresentativeId?.Trim();
            var trName = settlementStatementRequest.TeamRepresentativeName?.Trim();
            var program = settlementStatementRequest.ProgramName?.Trim();

            // Query AwardSettlements with filters and include navigation props
            var settlements = await _awardSettlementRepository.FindAsync(
                s =>
                    s.MonthStart >= start &&
                    s.MonthStart <= end &&
                    (string.IsNullOrWhiteSpace(trId) || (s.TeamRepresentative != null && s.TeamRepresentative.ExternalId == trId)) &&
                    (string.IsNullOrWhiteSpace(trName) || (s.TeamRepresentative != null && (s.TeamRepresentative.Name ?? "").Contains(trName))) &&
                    // Assuming ProgramName maps to TeamRepresentative.Segment (adjust if your model differs)
                    (string.IsNullOrWhiteSpace(program) || (s.TeamRepresentative != null && (s.TeamRepresentative.Segment ?? "") == program)),
                s => s.Member!,
                s => s.TeamRepresentative!
            );

            // Map to response
            var results = settlements
                .Select(s => new SettlementStatementResponse
                {
                    MemberId = s.Member?.MemberCode ?? string.Empty,
                    MemberName = s.Member?.FullName ?? string.Empty,
                    JoinedDate = s.JoinedDate.ToDateTime(TimeOnly.MinValue),
                    LastGamingDate = s.LastGamingDate.ToDateTime(TimeOnly.MinValue),
                    Eligible = s.Eligible,
                    CasinoWinLoss = s.CasinoWinLoss
                })
                .ToList();

            return results;
        }
    }
}
