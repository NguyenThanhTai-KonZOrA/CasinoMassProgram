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
            var results = await _statementService.SettlementStatementSearch(settlementStatementRequest);
            return Ok(results);
        }
    }
}
