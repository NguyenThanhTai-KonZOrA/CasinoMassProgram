using Implement.Services.Interface;
using Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace CasinoMassProgram.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettlementStatementController : ControllerBase
    {
        private readonly ISettlementStatementService _statementService;
        public SettlementStatementController(ISettlementStatementService statementService)
        {
            _statementService = statementService;
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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("list-teamRepresentatives")]
        public async Task<IActionResult> GetTeamRepresentatives(TeamRepresentativesRequest request)
        {
            try
            {
                var response = await _statementService.GetTeamRepresentatives(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("payment")]
        public async Task<IActionResult> PaymentTeamRepresentatives(PaymentTeamRepresentativesRequest paymentTeam)
        {
            try
            {
                var response = await _statementService.PaymentTeamRepresentatives(paymentTeam);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
