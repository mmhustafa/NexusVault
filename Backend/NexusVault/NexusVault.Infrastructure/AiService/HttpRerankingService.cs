using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace NexusVault.Infrastructure.AiService
{
    public class HttpRerankingService : IRerankingService
    {
        private readonly HttpClient _httpClient;

        public HttpRerankingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<RerankedResult>> RerankAsync(
            string query,
            IReadOnlyList<RerankCandidate> candidates,
            CancellationToken ct = default)
        {
            if (candidates.Count == 0)
                return Array.Empty<RerankedResult>();

            var response = await _httpClient.PostAsJsonAsync("/rerank", new
            {
                query,
                candidates = candidates.Select(c => new { id = c.Id.ToString(), text = c.Text })
            }, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RerankResponse>(AiServiceJsonOptions.Default, ct);

            return result!.Ranked
                .Select(r => new RerankedResult(Guid.Parse(r.Id), r.Score))
                .ToList();
        }

        private record RerankResponse(List<RerankedItem> Ranked);
        private record RerankedItem(string Id, double Score);

    }
}
