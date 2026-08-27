using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;
using NexusVault.Domain.Entities;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireTenantContext")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentIngestionService _ingestionService;
        private readonly ICurrentTenant _currentTenant;

        public DocumentsController(DocumentIngestionService ingestionService, ICurrentTenant currentTenant)
        {
            _ingestionService = ingestionService;
            _currentTenant = currentTenant;
        }

        [HttpPost]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [Authorize(Roles = TenantRoles.Admin)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, CancellationToken ct)
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("A non-empty file is required.");

            try
            {
                await using var stream = request.File.OpenReadStream();
                var result = await _ingestionService.UploadAsync(
                    _currentTenant.TenantId, _currentTenant.UserId, request.Title,
                    request.File.FileName, request.File.ContentType, request.File.Length, stream, ct);

                return AcceptedAtAction(
                    nameof(GetStatus),
                    new { documentVersionId = result.DocumentVersionId },
                    result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{documentVersionId:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid documentVersionId, CancellationToken ct)
        {
            var status = await _ingestionService.GetStatusAsync(documentVersionId,_currentTenant.TenantId, ct);
            return status is null ? NotFound() : Ok(status);
        }

    }
}
