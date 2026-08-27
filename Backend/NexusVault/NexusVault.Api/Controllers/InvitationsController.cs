using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using NexusVault.Domain.Entities;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireTenantContext", Roles = TenantRoles.Admin)]
    public class InvitationsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentTenant _currentTenant;

        public InvitationsController(IAuthService authService, ICurrentTenant currentTenant)
        {
            _authService = authService;
            _currentTenant = currentTenant;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _authService.CreateInvitationAsync(
                    _currentTenant.TenantId, _currentTenant.UserId, request.Email,
                    request.Role ?? TenantRoles.User, ct);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
