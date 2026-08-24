using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace NexusVault.Infrastructure.AiService
{
    public class HttpRagSynthesisService : IRagSynthesisService
    {
        private readonly HttpClient _httpClient;

        public HttpRagSynthesisService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RagSynthesisResult> SynthesizeAsync(
            string query,
            IReadOnlyList<RagSourceChunk> chunks,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/rag-synthesize", new
            {
                query,
                chunks = chunks.Select(c => new { id = c.Id.ToString(), text = c.Text })
            }, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RagSynthesizeResponse>(AiServiceJsonOptions.Default, ct);

            
            var citedIds = result!.CitedChunkIds
                .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
                .Where(guid => guid.HasValue)
                .Select(guid => guid!.Value)
                .ToList();

            return new RagSynthesisResult(result.Answer, citedIds);
        }

        private record RagSynthesizeResponse(string Answer, List<string> CitedChunkIds);
    }

}

