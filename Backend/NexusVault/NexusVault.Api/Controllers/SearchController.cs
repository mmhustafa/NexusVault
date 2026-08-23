using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Services;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;

        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken ct)
        {
            // TODO(Phase 8): replace with tenantId resolved from JWT claims via
            // ICurrentTenant, once auth is wired up -- same placeholder pattern
            // as DocumentsController.
            var tenantId = Guid.Empty;

            try
            {
                var results = await _searchService.SearchAsync(
                    request.Query, tenantId, request.TopK, request.DocumentId, request.Mode, request.Fusion, request.Rerank, ct);
                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
