using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace NexusVault.Infrastructure.AiService
{
    public class HttpEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;

        public HttpEmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EmbedResult> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/embed", new
            {
                texts
            }, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(AiServiceJsonOptions.Default, ct);

            var vectors = result!.Vectors
                .Select(v => v.ToArray())
                .ToArray();

            return new EmbedResult(vectors, result.ModelName, result.ModelVersion, result.Dimensions);
        }

        // Internal deserialization shapes -- mirror the FastAPI response schema
        private record EmbedResponse(
            List<List<float>> Vectors,
            string ModelName,
            string ModelVersion,
            int Dimensions);
    }
}
