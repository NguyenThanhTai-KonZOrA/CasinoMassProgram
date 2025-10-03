using Common.Enums;
using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;
using Implement.Services.Interface;
using Implement.UnitOfWork;
using Implement.ViewModels.Request;
using Implement.ViewModels.Response;
using Microsoft.EntityFrameworkCore;

namespace Implement.Services
{
    public class SettlementStatementService : ISettlementStatementService
    {
        private readonly IAwardSettlementRepository _awardSettlementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly CasinoMassProgramDbContext _dbContext;

        public SettlementStatementService(IAwardSettlementRepository awardSettlementRepository,
            CasinoMassProgramDbContext dbContext,
            IUnitOfWork unitOfWork)
        {
            _awardSettlementRepository = awardSettlementRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
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

            var settlements = await _awardSettlementRepository.FindAsync(x => x.JoinedDate >= start && x.LastGamingDate <= end,
                s => s.Member!,
                s => s.TeamRepresentative!);

            if (!string.IsNullOrEmpty(settlementStatementRequest.TeamRepresentativeId))
            {
                string? seamRepresentativeId = settlementStatementRequest.TeamRepresentativeId?.Trim();
                settlements = settlements.Where(x => x.TeamRepresentative?.TeamRepresentativeId == seamRepresentativeId);
            }

            if (!string.IsNullOrEmpty(settlementStatementRequest.TeamRepresentativeName))
            {
                string? teamRepresentativeName = settlementStatementRequest.TeamRepresentativeName?.Trim();
                settlements = settlements.Where(x => x.TeamRepresentative?.TeamRepresentativeName == teamRepresentativeName);

            }

            if (!string.IsNullOrEmpty(settlementStatementRequest.ProgramName))
            {
                string programName = settlementStatementRequest.ProgramName.Trim();
                settlements = settlements.Where(x => x.TeamRepresentative?.Segment == programName);
            }
            settlements = settlements.Distinct();

            // Map to response
            var results = settlements
                .Select(s => new SettlementStatementResponse
                {
                    SettlementId = s.Id, // ensure uniqueness for React key
                    MemberId = s.Member?.MemberCode ?? string.Empty,
                    MemberName = s.Member?.FullName ?? string.Empty,
                    JoinedDate = s.JoinedDate.ToDateTime(TimeOnly.MinValue),
                    LastGamingDate = s.LastGamingDate.ToDateTime(TimeOnly.MinValue),
                    Eligible = s.Eligible,
                    CasinoWinLoss = s.CasinoWinLoss
                }).OrderByDescending(x => x.JoinedDate)
                .ToList();

            return results;
        }

        public async Task<List<TeamRepresentativesResponse>> GetTeamRepresentatives(TeamRepresentativesRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = _dbContext.AwardSettlements
                .AsNoTracking()
                .AsQueryable();

            if (request.Month.HasValue)
            {
                var m = request.Month.Value;
                var monthStart = new DateOnly(m.Year, m.Month, 1);
                query = query.Where(s => s.MonthStart == monthStart);
            }

            if (!string.IsNullOrWhiteSpace(request.TeamRepresentativeId))
            {
                var trId = request.TeamRepresentativeId.Trim();
                query = query.Where(s => s.TeamRepresentative != null && s.TeamRepresentative.TeamRepresentativeId == trId);
            }

            if (!string.IsNullOrWhiteSpace(request.TeamRepresentativeName))
            {
                var trName = request.TeamRepresentativeName.Trim();
                query = query.Where(s => s.TeamRepresentative != null && s.TeamRepresentative.TeamRepresentativeName == trName);
            }

            if (!string.IsNullOrWhiteSpace(request.ProgramName))
            {
                var program = request.ProgramName.Trim();
                query = query.Where(s => s.TeamRepresentative != null && s.TeamRepresentative.Segment == program);
            }

            var aggregates = await query
                .GroupBy(s => new
                {
                    s.MonthStart,
                    ExternalId = s.TeamRepresentative!.TeamRepresentativeId,
                    Name = s.TeamRepresentative!.TeamRepresentativeName,
                    Segment = s.TeamRepresentative!.Segment
                })
                .Select(g => new
                {
                    g.Key.MonthStart,
                    TeamRepresentativeName = g.Key.Name,
                    TeamRepresentativeExternalId = g.Key.ExternalId,
                    ProgramName = g.Key.Segment,
                    AwardTotal = g.Sum(x => x.AwardSettlementAmount),
                    Segment = g.Key.Segment,
                    CasinoWinLoss = g.Sum(x => x.CasinoWinLoss),
                    SettlementDoc = g.Max(x => x.SettlementDoc),
                    Status = _dbContext.PaymentTeamRepresentatives
                        .Where(p =>
                            p.MonthStart == g.Key.MonthStart &&
                            p.TeamRepresentativeId ==
                                _dbContext.TeamRepresentatives
                                    .Where(tr => tr.TeamRepresentativeId == g.Key.ExternalId)
                                    .Select(tr => tr.Id)
                                    .FirstOrDefault()
                        )
                        .Select(p => p.Status)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.MonthStart)
                .ThenBy(x => x.TeamRepresentativeName)
                .ToListAsync();

            var result = aggregates.Select(x => new TeamRepresentativesResponse
            {
                Segment = x.Segment,
                CasinoWinLoss = x.CasinoWinLoss,
                SettlementDoc = x.SettlementDoc,
                TeamRepresentativeName = x.TeamRepresentativeName ?? string.Empty,
                TeamRepresentativeId = x.TeamRepresentativeExternalId ?? string.Empty,
                ProgramName = x.ProgramName ?? string.Empty,
                Month = x.MonthStart.ToDateTime(TimeOnly.MinValue),
                AwardTotal = x.AwardTotal,
                Status = x.Status ?? string.Empty,
                IsPayment = string.Equals(x.Status, "Void", StringComparison.OrdinalIgnoreCase)
            }).ToList();

            return result;
        }

        public async Task<PaymentTeamRepresentativesResponse> PaymentTeamRepresentatives(PaymentTeamRepresentativesRequest paymentTeam)
        {
            if (paymentTeam == null) throw new ArgumentNullException(nameof(paymentTeam));
            if (!paymentTeam.Month.HasValue) throw new ArgumentException("Month is required.", nameof(paymentTeam.Month));

            // Resolve TeamRepresentative by ExternalId (preferred) or by Name
            var teamRepQuery = _dbContext.TeamRepresentatives.AsQueryable();
            if (!string.IsNullOrWhiteSpace(paymentTeam.TeamRepresentativeId))
            {
                var teamRepresentativeId = paymentTeam.TeamRepresentativeId.Trim();
                teamRepQuery = teamRepQuery.Where(tr => tr.TeamRepresentativeId == teamRepresentativeId);
            }
            else if (!string.IsNullOrWhiteSpace(paymentTeam.TeamRepresentativeName))
            {
                var name = paymentTeam.TeamRepresentativeName.Trim();
                teamRepQuery = teamRepQuery.Where(tr => tr.TeamRepresentativeName == name);
            }
            else
            {
                throw new ArgumentException("TeamRepresentativeId or TeamRepresentativeName is required.");
            }

            var teamRep = await teamRepQuery.FirstOrDefaultAsync();
            if (teamRep == null) throw new InvalidOperationException("TeamRepresentative not found.");

            var month = paymentTeam.Month.Value;
            var monthStart = new DateOnly(month.Year, month.Month, 1);

            var existed = await _dbContext.PaymentTeamRepresentatives
                .AnyAsync(p => p.TeamRepresentativeId == teamRep.Id && p.MonthStart == monthStart && p.Status == "Void");
            if (existed)
            {
                return new PaymentTeamRepresentativesResponse { IsPayment = false };
            }

            var awardTotal = await _dbContext.AwardSettlements
                .Where(s => s.TeamRepresentativeId == teamRep.Id && s.MonthStart == monthStart)
                .SumAsync(s => (decimal?)s.AwardSettlementAmount) ?? 0m;

            var payment = new PaymentTeamRepresentative
            {
                Id = Guid.NewGuid(),
                TeamRepresentativeId = teamRep.Id,
                MonthStart = monthStart,
                AwardTotal = awardTotal,
                Status = PaymentProcessEnum.Inprocess.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PaymentTeamRepresentative.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            try
            {
                payment.Status = PaymentProcessEnum.Voided.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentTeamRepresentativesResponse
                {
                    IsPayment = true
                };
            }
            catch
            {
                payment.Status = PaymentProcessEnum.Falied.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentTeamRepresentativesResponse
                {
                    IsPayment = false
                };
            }
        }
    }
}
