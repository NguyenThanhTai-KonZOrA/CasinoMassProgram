using Common.Constants;
using Common.Enums;
using DocumentFormat.OpenXml.Wordprocessing;
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
        private readonly IPaymentTeamRepresentativeRepository _paymentTeamRepresentativeRepository;

        public SettlementStatementService(IAwardSettlementRepository awardSettlementRepository,
            CasinoMassProgramDbContext dbContext,
            IUnitOfWork unitOfWork,
            IPaymentTeamRepresentativeRepository paymentTeamRepresentativeRepository)
        {
            _awardSettlementRepository = awardSettlementRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _paymentTeamRepresentativeRepository = paymentTeamRepresentativeRepository;
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

            var query = _dbContext.AwardSettlements.Where(x => x.IsActive)
                .AsNoTracking()
                .AsQueryable();

            //if (request.Month.HasValue)
            //{
            //    var m = request.Month.Value;
            //    var monthStart = new DateOnly(m.Year, m.Month, 1);
            //    //query = query.Where(s => s.MonthStart == monthStart);
            //}

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
                    TeamRepresentativeId = s.TeamRepresentative!.TeamRepresentativeId,
                    Name = s.TeamRepresentative!.TeamRepresentativeName,
                    Segment = s.TeamRepresentative!.Segment
                })
                .Select(g => new
                {
                    g.Key.MonthStart,
                    TeamRepresentativeName = g.Key.Name,
                    TeamRepresentativeId = g.Key.TeamRepresentativeId,
                    ProgramName = g.Key.Segment,
                    AwardTotal = g.Sum(x => x.AwardSettlementAmount),
                    Segment = g.Key.Segment,
                    CasinoWinLoss = g.Sum(x => x.CasinoWinLoss),
                    SettlementDoc = g.Max(x => x.SettlementDoc),
                    PaymentTeamRepresentatives = _dbContext.PaymentTeamRepresentatives
                        .Where(p =>
                            p.MonthStart == g.Key.MonthStart && //p.Status == request.Status &&
                            p.TeamRepresentativeId ==
                                _dbContext.TeamRepresentatives
                                    .Where(tr => tr.TeamRepresentativeId == g.Key.TeamRepresentativeId)
                                    .Select(tr => tr.Id)
                                    .FirstOrDefault()
                        )
                        .Select(p => new { p.Status, p.Id, p.IsPrintf, p.CreatedBy, p.CreatedAt })
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
                TeamRepresentativeId = x.TeamRepresentativeId ?? string.Empty,
                ProgramName = x.ProgramName ?? string.Empty,
                Month = x.MonthStart.ToDateTime(TimeOnly.MinValue),
                AwardTotal = x.AwardTotal,
                Status = x.PaymentTeamRepresentatives?.Status ?? string.Empty,
                PaymentTeamRepresentativesId = x.PaymentTeamRepresentatives != null ? x.PaymentTeamRepresentatives.Id : Guid.Empty,
                IsPayment = string.Equals(x.PaymentTeamRepresentatives?.Status, PaymentProcessEnum.Paid.ToString(), StringComparison.OrdinalIgnoreCase),
                IsPrintf = x.PaymentTeamRepresentatives != null ? x.PaymentTeamRepresentatives.IsPrintf : false,
                PaymentBy = x.PaymentTeamRepresentatives?.CreatedBy ?? string.Empty,
                PaymentDate = x.PaymentTeamRepresentatives != null ? x.PaymentTeamRepresentatives.CreatedAt : DateTime.MinValue
            }).ToList();

            return result;
        }

        public async Task<List<TeamRepresentativesResponse>> GetTeamRepresentativesV2(TeamRepresentativesRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var query = _dbContext.PaymentTeamRepresentatives.Where(x => x.IsActive)
                .AsNoTracking()
                .AsQueryable();

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

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();
                query = query.Where(s => s.Status == status);
            }

            var payments = await query
                .Include(p => p.TeamRepresentative)
                .OrderByDescending(x => x.MonthStart)
                .ThenBy(x => x.TeamRepresentative!.TeamRepresentativeName)
                .ToListAsync();

            var result = payments.Select(x => new TeamRepresentativesResponse
            {
                Segment = x.TeamRepresentative?.Segment,
                CasinoWinLoss = x.CasinoWinLossTotal,
                SettlementDoc = x.SettlementDoc,
                TeamRepresentativeName = x.TeamRepresentative?.TeamRepresentativeName ?? string.Empty,
                TeamRepresentativeId = x.TeamRepresentative?.TeamRepresentativeId ?? string.Empty,
                Month = x.MonthStart.ToDateTime(TimeOnly.MinValue),
                AwardTotal = x.AwardTotal,
                Status = x.Status,
                PaymentTeamRepresentativesId = x.Id,
                IsPayment = string.Equals(x.Status, PaymentProcessEnum.Paid.ToString(), StringComparison.OrdinalIgnoreCase),
                IsPrintf = x.IsPrintf,
                PaymentBy = x.UpdatedBy ?? string.Empty,
                PaymentDate = x.UpdatedAt
            }).ToList();

            return result;

        }
        public async Task<PaymentTeamRepresentativesResponse> PaymentTeamRepresentatives(PaymentTeamRepresentativesRequest paymentTeam, string userName)
        {
            if (paymentTeam == null) throw new ArgumentNullException(nameof(paymentTeam));
            if (!paymentTeam.Month.HasValue) throw new ArgumentException("Month is required.", nameof(paymentTeam.Month));

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
            if (teamRep == null) throw new ArgumentException("TeamRepresentative not found.");

            var month = paymentTeam.Month.Value;
            var monthStart = new DateOnly(month.Year, month.Month, 1);

            var queryPaid = _dbContext.PaymentTeamRepresentatives.Where(p => p.TeamRepresentativeId == teamRep.Id && p.MonthStart == monthStart);
            var existedPaid = await queryPaid.AnyAsync(x => x.Status == PaymentProcessEnum.Paid.ToString());
            if (existedPaid)
            {
                return new PaymentTeamRepresentativesResponse { IsPayment = false };
            }

            var casinoWinLoss = await _dbContext.AwardSettlements
                .Where(s => s.TeamRepresentativeId == teamRep.Id && s.MonthStart == monthStart)
                .SumAsync(s => (decimal?)s.CasinoWinLoss) ?? 0m;
            var awardTotal = CalculateAwardTotal(casinoWinLoss);

            var existedVoided = await queryPaid.FirstOrDefaultAsync(x => x.Status == PaymentProcessEnum.Voided.ToString());

            PaymentTeamRepresentative payment = new();

            if (existedVoided != null)
            {
                payment = existedVoided;
                payment.Status = PaymentProcessEnum.Inprocess.ToString();
                payment.UpdatedBy = userName ?? CommonContants.SystemUser;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
            }
            else
            {
                payment = new PaymentTeamRepresentative
                {
                    Id = Guid.NewGuid(),
                    TeamRepresentativeId = teamRep.Id,
                    MonthStart = monthStart,
                    AwardTotal = awardTotal,
                    Status = PaymentProcessEnum.Inprocess.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userName ?? CommonContants.SystemUser,
                    UpdatedBy = userName ?? CommonContants.SystemUser,
                    IsPrintf = false
                };
                await _unitOfWork.PaymentTeamRepresentative.AddAsync(payment);
            }

            await _unitOfWork.CompleteAsync();

            try
            {
                payment.Status = PaymentProcessEnum.Paid.ToString();
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

        public async Task<PaymentTeamRepresentativesResponse> PaymentTeamRepresentativesV2(PaymentTeamRepresentativesRequest paymentTeam, string currentUserName)
        {
            var payment = await _paymentTeamRepresentativeRepository
                                .FirstOrDefaultAsync(p => p.Id == paymentTeam.PaymentTeamRepresentativesId &&
                                (p.Status == PaymentProcessEnum.Pending.ToString() ||
                                 p.Status == PaymentProcessEnum.Voided.ToString()));

            if (payment == null) return new PaymentTeamRepresentativesResponse { IsPayment = false };

            payment.Status = PaymentProcessEnum.Inprocess.ToString();
            payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
            _unitOfWork.PaymentTeamRepresentative.Update(payment);
            await _unitOfWork.CompleteAsync();

            try
            {
                payment.Status = PaymentProcessEnum.Paid.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentTeamRepresentativesResponse
                {
                    IsPayment = true,
                    PaymentTeamRepresentativesId = payment.Id
                };
            }
            catch
            {
                payment.Status = PaymentProcessEnum.Falied.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentTeamRepresentativesResponse
                {
                    IsPayment = false,
                    PaymentTeamRepresentativesId = payment.Id
                };
            }
        }

        public async Task<UnPaidTeamRepresentativesResponse> UnPaidTeamRepresentatives(UnPaidTeamRepresentativesRequest unPaidTeam, string currentUserName)
        {
            var payment = await _paymentTeamRepresentativeRepository.FirstOrDefaultAsync(p => p.Id == unPaidTeam.PaymentTeamRepresentativesId && p.Status == PaymentProcessEnum.Paid.ToString());

            if (payment == null)
            {
                return new UnPaidTeamRepresentativesResponse { IsUnPaid = false };
            }

            payment.Status = PaymentProcessEnum.Inprocess.ToString();
            payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
            _unitOfWork.PaymentTeamRepresentative.Update(payment);
            await _unitOfWork.CompleteAsync();

            try
            {
                payment.Status = PaymentProcessEnum.Voided.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new UnPaidTeamRepresentativesResponse { IsUnPaid = true };
            }
            catch
            {
                payment.Status = PaymentProcessEnum.Falied.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = currentUserName ?? CommonContants.SystemUser;
                _unitOfWork.PaymentTeamRepresentative.Update(payment);
                await _unitOfWork.CompleteAsync();

                return new UnPaidTeamRepresentativesResponse { IsUnPaid = true };
            }
        }
        private decimal CalculateAwardTotal(decimal casinoWinLoss)
        {
            if (casinoWinLoss >= 90000m)
            {
                return casinoWinLoss * 0.12m;
            }
            else if (casinoWinLoss >= 3000m)
            {
                return casinoWinLoss * 0.010m;
            }
            else if (casinoWinLoss >= 1000m)
            {
                return casinoWinLoss * 0.05m;
            }
            else
            {
                return 0m;
            }
        }
    }
}
