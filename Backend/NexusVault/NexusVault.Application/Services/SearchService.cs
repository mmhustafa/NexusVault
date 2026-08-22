using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Services
{
    public class SearchService
    {
        private const int DefaultTopK = 5;
        private const int MaxTopK = 20;

        private readonly IEmbeddingService _embeddingService;
        private readonly IChunkSearchRepository _searchRepository;

        public SearchService(IEmbeddingService embeddingService, IChunkSearchRepository searchRepository)
        {
            _embeddingService = embeddingService;
            _searchRepository = searchRepository;
        }

        public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
            string query,
            Guid tenantId,
            int? topK,
            Guid? documentId = null,
            SearchMode mode = SearchMode.Dense,
            CancellationToken ct = default)
        {

            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidOperationException("Query text is required.");

            var effectiveTopK = Math.Clamp(topK ?? DefaultTopK, 1, MaxTopK);

            return mode switch
            {
                SearchMode.Sparse => await SearchSparseAsync(query, tenantId, effectiveTopK, documentId, ct),
                _ => await SearchDenseAsync(query, tenantId, effectiveTopK, documentId, ct)
            };
        }

        private async Task<IReadOnlyList<SearchResultDto>> SearchDenseAsync(
            string query, Guid tenantId, int topK, Guid? documentId, CancellationToken ct)
        {
            
            var embedResult = await _embeddingService.EmbedAsync(new[] { query }, ct);
            var queryVector = embedResult.Vectors[0];

            var rows = await _searchRepository.FindNearestAsync(queryVector, tenantId, topK, documentId, ct);

            return rows
                .Select(r => new SearchResultDto(
                    r.ChunkId, r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading,
                    Score: 1 - r.Distance)) 
                .ToList();
        }

        private async Task<IReadOnlyList<SearchResultDto>> SearchSparseAsync(
            string query, Guid tenantId, int topK, Guid? documentId, CancellationToken ct)
        {

            var rows = await _searchRepository.FindByFullTextAsync(query, tenantId, topK, documentId, ct);

            return rows
                .Select(r => new SearchResultDto(
                    r.ChunkId, r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading,
                    Score: r.Rank)) 
                .ToList();
        }
    }
}
