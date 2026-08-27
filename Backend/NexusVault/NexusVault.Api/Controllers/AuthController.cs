using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _authService.RegisterAsync(request.Email, request.Password, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _authService.LoginAsync(request.Email, request.Password, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("select-tenant")]
        [Authorize]
        public async Task<IActionResult> SelectTenant([FromBody] SelectTenantRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromClaims();
            if (userId is null) return Unauthorized("Invalid token.");

            try
            {
                var tokens = await _authService.SelectTenantAsync(userId.Value, request.TenantId, ct);
                return Ok(tokens);
            }
            catch (InvalidOperationException)
            {
                return Forbid(); 
            }
        }

        [HttpPost("create-workspace")]
        [Authorize]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromClaims();
            if (userId is null) return Unauthorized("Invalid token.");

            try
            {
                var tokens = await _authService.CreateWorkspaceAsync(userId.Value, request.WorkspaceName, ct);
                return Ok(tokens);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("accept-invitation")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromClaims();
            if (userId is null) return Unauthorized("Invalid token.");

            try
            {
                var tokens = await _authService.AcceptInvitationAsync(userId.Value, request.InvitationToken, ct);
                return Ok(tokens);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        {
            try
            {
                var tokens = await _authService.RefreshAsync(request.RefreshToken, ct);
                return Ok(tokens);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        private Guid? GetUserIdFromClaims()
        {
            var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(subClaim, out var id) ? id : null;
        }
    }
}
