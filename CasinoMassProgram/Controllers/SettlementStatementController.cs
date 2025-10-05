using Common.CurrentUserLogin;
using Implement.Services.Interface;
using Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace CasinoMassProgram.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettlementStatementController : BaseApiController
    {
        private readonly ISettlementStatementService _statementService;
        private readonly ICurrentUserService _currentUser;
        public SettlementStatementController(ISettlementStatementService statementService, ICurrentUserService currentUser)
        {
            _statementService = statementService;
            _currentUser = currentUser;
        }

        [HttpPost("settlement-statement-search")]
        public async Task<IActionResult> SettlementStatementSearch(SettlementStatementRequest settlementStatementRequest)
        {
            try
            {
                var results = await _statementService.SettlementStatementSearch(settlementStatementRequest);
                return Ok(results);
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException(ex.Message);
            }
        }

        [HttpPost("list-teamRepresentatives")]
        public async Task<IActionResult> GetTeamRepresentatives(TeamRepresentativesRequest request)
        {
            try
            {
                var response = await _statementService.GetTeamRepresentativesV2(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException(ex.Message);
            }
        }

        [HttpPost("payment")]
        public async Task<IActionResult> PaymentTeamRepresentatives(PaymentTeamRepresentativesRequest paymentTeam)
        {
            try
            {
                var response = await _statementService.PaymentTeamRepresentativesV2(paymentTeam, _currentUser.UserName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException(ex.Message);
            }
        }

        [HttpPost("unPaid")]
        public async Task<IActionResult> UnPaidTeamRepresentatives(UnPaidTeamRepresentativesRequest unPaidTeam)
        {
            try
            {
                var response = await _statementService.UnPaidTeamRepresentatives(unPaidTeam, _currentUser.UserName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException(ex.Message);
            }
        }
    }
}
