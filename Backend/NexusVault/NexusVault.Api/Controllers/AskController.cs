using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AskController : ControllerBase
    {
        private readonly AskService _askService;
        private readonly ICurrentTenant _currentTenant;

        public AskController(AskService askService, ICurrentTenant currentTenant)
        {
            _askService = askService;
            _currentTenant = currentTenant;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
        {

            try
            {
                var result = await _askService.AskAsync(request.Query, _currentTenant.TenantId, request.DocumentId, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
