using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.Services;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentIngestionService _ingestionService;

        public DocumentsController(DocumentIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        [HttpPost]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, CancellationToken ct)
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("A non-empty file is required.");

            // TODO(Phase 8): replace with tenantId/userId resolved from JWT claims
            // via ICurrentTenant / ICurrentUser, once auth is wired up.
            var tenantId = Guid.Empty;
            var userId = Guid.Empty;

            try
            {
                await using var stream = request.File.OpenReadStream();
                var result = await _ingestionService.UploadAsync(
                    tenantId, userId, request.Title, request.File.FileName, request.File.ContentType, request.File.Length, stream, ct);

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
            var status = await _ingestionService.GetStatusAsync(documentVersionId, ct);
            return status is null ? NotFound() : Ok(status);
        }

    }
}
