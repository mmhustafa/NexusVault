using Microsoft.Extensions.Logging;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Services
{
    public class AskService
    {
        private const int RagTopK = 5;

        private readonly SearchService _searchService;
        private readonly IRagSynthesisService _ragSynthesisService;
        private readonly ILogger<AskService> _logger;

        public AskService(
            SearchService searchService,
            IRagSynthesisService ragSynthesisService,
            ILogger<AskService> logger)
        {
            _searchService = searchService;
            _ragSynthesisService = ragSynthesisService;
            _logger = logger;
        }

        public async Task<AskResultDto> AskAsync(
            string query,
            Guid tenantId,
            Guid? documentId = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidOperationException("Query text is required.");

            var searchResults = await _searchService.SearchAsync(
                query, tenantId, RagTopK, documentId,
                mode: SearchMode.Hybrid, fusion: FusionStrategy.Rrf, rerank: true, ct: ct);

            if (searchResults.Count == 0)
            {
                return new AskResultDto(
                    "I don't have any relevant information to answer that question.",
                    Array.Empty<CitationDto>());
            }

            var sourceChunks = searchResults
                .Select(r => new RagSourceChunk(r.ChunkId, r.Text))
                .ToList();

            var synthesis = await _ragSynthesisService.SynthesizeAsync(query, sourceChunks, ct);

            // Citation validation -- never trust the LLM's cited ids blindly.
            // Every cited id must correspond to a chunk that was ACTUALLY
            // provided as context.

            var validChunkIds = new HashSet<Guid>(searchResults.Select(r => r.ChunkId));
            var invalidCitations = synthesis.CitedChunkIds.Where(id => !validChunkIds.Contains(id)).ToList();

            if (invalidCitations.Count > 0)
            {
                _logger.LogWarning(
                    "RAG response cited {InvalidCount} chunk id(s) not present in the provided context: {InvalidIds}",
                    invalidCitations.Count, string.Join(", ", invalidCitations));

                throw new InvalidOperationException(
                    "The generated answer referenced sources that were not part of the retrieved context and could not be verified. Please try rephrasing your question.");
            }

            var citations = searchResults
                .Where(r => synthesis.CitedChunkIds.Contains(r.ChunkId))
                .Select(r => new CitationDto(r.ChunkId, r.DocumentId, r.DocumentTitle, r.PageNumber, r.SectionHeading))
                .ToList();

            return new AskResultDto(synthesis.Answer, citations);
        }
    }
}
