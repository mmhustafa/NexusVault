using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace NexusVault.Infrastructure.AiService
{
    public class HttpChunkingService: IChunkingService
    {
        private readonly HttpClient _httpClient;

        public HttpChunkingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<ChunkDto>> ChunkAsync(
            string text,
            int maxTokensPerChunk = 300,
            CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/chunk", new
            {
                text,
                max_tokens_per_chunk = maxTokensPerChunk
            }, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChunkResponse>(AiServiceJsonOptions.Default, ct);

            return result!.Chunks
                .Select(c => new ChunkDto(c.ChunkIndex, c.Text, c.PageNumber, c.SectionHeading))
                .ToList();
        }

        // Internal deserialization shapes -- mirror the FastAPI response schema
        private record ChunkResponse(List<ChunkItem> Chunks);
        private record ChunkItem(
            int ChunkIndex,
            string Text,
            int? PageNumber,
            string? SectionHeading);
    }
}
