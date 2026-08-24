using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Services;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AskController : ControllerBase
    {
        private readonly AskService _askService;

        public AskController(AskService askService)
        {
            _askService = askService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
        {
            // TODO(Phase 8): replace with tenantId resolved from JWT claims,
            // same placeholder pattern as every other controller so far.
            var tenantId = Guid.Empty;

            try
            {
                var result = await _askService.AskAsync(request.Query, tenantId, request.DocumentId, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
