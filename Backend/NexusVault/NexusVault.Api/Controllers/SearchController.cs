using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;

namespace NexusVault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireTenantContext")]

    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;
        private readonly ICurrentTenant _currentTenant;

        public SearchController(SearchService searchService, ICurrentTenant currentTenant)
        {
            _searchService = searchService;
            _currentTenant = currentTenant;
        }

        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken ct)
        {
            
            try
            {
                var results = await _searchService.SearchAsync(
                    request.Query, _currentTenant.TenantId, request.TopK, request.DocumentId, request.Mode, request.Fusion, request.Rerank, ct);
                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
