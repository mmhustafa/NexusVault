using Microsoft.Extensions.Logging;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NexusVault.Application.Services
{
    public class SearchService
    {
        private const int DefaultTopK = 5;
        private const int MaxTopK = 20;
        private const int RrfK = 60;

        private const int RerankPoolMultiplier = 5;
        private const int MinRerankPoolSize = 20;

        private readonly IEmbeddingService _embeddingService;
        private readonly IChunkSearchRepository _searchRepository;
        private readonly IRerankingService _rerankingService;
        private readonly ILogger<SearchService> _logger;

        public SearchService(IEmbeddingService embeddingService, IChunkSearchRepository searchRepository, IRerankingService rerankingService, ILogger<SearchService> logger)
        {
            _embeddingService = embeddingService;
            _searchRepository = searchRepository;
            _rerankingService = rerankingService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
            string query,
            Guid tenantId,
            int? topK,
            Guid? documentId = null,
            SearchMode mode = SearchMode.Dense,
            FusionStrategy fusion = FusionStrategy.Rrf,
            bool rerank = false,
            bool includeArchived = false,
            CancellationToken ct = default)
        {

            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidOperationException("Query text is required.");

            var effectiveTopK = Math.Clamp(topK ?? DefaultTopK, 1, MaxTopK);
            var retrievalPoolSize = rerank
                ? Math.Max(effectiveTopK * RerankPoolMultiplier, MinRerankPoolSize)
                : effectiveTopK;

            var retrievalStopwatch = Stopwatch.StartNew();

            var candidates = mode switch
            {
                SearchMode.Sparse => await SearchSparseAsync(query, tenantId, retrievalPoolSize, documentId, includeArchived, ct),
                SearchMode.Hybrid => await SearchHybridAsync(query, tenantId, retrievalPoolSize, documentId, fusion, includeArchived, ct),
                _ => await SearchDenseAsync(query, tenantId, retrievalPoolSize, documentId,includeArchived, ct)
            };

            retrievalStopwatch.Stop();
            _logger.LogInformation(
                "Retrieval ({Mode}, pool={PoolSize}) took {ElapsedMs}ms, {CandidateCount} candidates",
                mode, retrievalPoolSize, retrievalStopwatch.ElapsedMilliseconds, candidates.Count);

            if (!rerank || candidates.Count == 0)
                return candidates.Take(effectiveTopK).ToList();

            var rerankStopwatch = Stopwatch.StartNew();
            var result = await RerankResultsAsync(query, candidates, effectiveTopK, ct);
            rerankStopwatch.Stop();

            _logger.LogInformation(
                "Reranking took {ElapsedMs}ms for {CandidateCount} candidates",
                rerankStopwatch.ElapsedMilliseconds, candidates.Count);

            return result;
        }

        private async Task<IReadOnlyList<SearchResultDto>> RerankResultsAsync(
        string query, IReadOnlyList<SearchResultDto> candidates, int topK, CancellationToken ct)
        {
            var rerankCandidates = candidates
                .Select(c => new RerankCandidate(c.ChunkId, c.Text))
                .ToList();

            var reranked = await _rerankingService.RerankAsync(query, rerankCandidates, ct);
            var byId = candidates.ToDictionary(c => c.ChunkId);

            return reranked
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .Select(r => byId[r.Id] with { Score = r.Score }) // cross-encoder score replaces the retrieval score
                .ToList();
        }
        private async Task<IReadOnlyList<SearchResultDto>> SearchDenseAsync(
            string query, Guid tenantId, int topK, Guid? documentId, bool includeArchived, CancellationToken ct)
        {

            var queryVector = await EmbedQueryAsync(query, ct);
            var rows = await _searchRepository.FindNearestAsync(queryVector, tenantId, topK, documentId,includeArchived, ct);

            return rows
                .Select(r => new SearchResultDto(
                    r.ChunkId, r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading,
                    Score: 1 - r.Distance)) 
                .ToList();
        }

        private async Task<IReadOnlyList<SearchResultDto>> SearchSparseAsync(
            string query, Guid tenantId, int topK, Guid? documentId, bool includeArchived, CancellationToken ct)
        {

            var rows = await _searchRepository.FindByFullTextAsync(query, tenantId, topK, documentId, includeArchived, ct);

            return rows
                .Select(r => new SearchResultDto(
                    r.ChunkId, r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading,
                    Score: r.Rank)) 
                .ToList();
        }

        private async Task<IReadOnlyList<SearchResultDto>> SearchHybridAsync(
            string query, Guid tenantId, int topK, Guid? documentId, FusionStrategy fusion, bool includeArchived, CancellationToken ct)
        {
            var candidatePoolSize = Math.Max(topK * 3, 20);

            var queryVector = await EmbedQueryAsync(query, ct);

            var denseResults = await _searchRepository.FindNearestAsync(queryVector, tenantId, candidatePoolSize, documentId, includeArchived, ct);
            var sparseResults = await _searchRepository.FindByFullTextAsync(query, tenantId, candidatePoolSize, documentId, includeArchived, ct);
            

            return fusion == FusionStrategy.WeightedSum
                ? FuseWithWeightedSum(denseResults, sparseResults, topK)
                : FuseWithRrf(denseResults, sparseResults, topK);
        }

        private async Task<float[]> EmbedQueryAsync(string query, CancellationToken ct)
        {
            var embedResult = await _embeddingService.EmbedAsync(new[] { query }, ct);
            return embedResult.Vectors[0];
        }

        private static List<SearchResultDto> FuseWithRrf(
           IReadOnlyList<ChunkSearchResult> denseResults,
           IReadOnlyList<ChunkFullTextResult> sparseResults,
           int topK)
        {
            var scores = new Dictionary<Guid, double>();
            var metadata = new Dictionary<Guid, ChunkMetadata>();

            for (var rank = 0; rank < denseResults.Count; rank++)
            {
                var r = denseResults[rank];
                scores[r.ChunkId] = scores.GetValueOrDefault(r.ChunkId) + 1.0 / (RrfK + rank + 1);
                metadata.TryAdd(r.ChunkId, new ChunkMetadata(r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading));
            }

            for (var rank = 0; rank < sparseResults.Count; rank++)
            {
                var r = sparseResults[rank];
                scores[r.ChunkId] = scores.GetValueOrDefault(r.ChunkId) + 1.0 / (RrfK + rank + 1);
                metadata.TryAdd(r.ChunkId, new ChunkMetadata(r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading));
            }

            return scores
                .OrderByDescending(kv => kv.Value)
                .Take(topK)
                .Select(kv => ToDto(kv.Key, kv.Value, metadata[kv.Key]))
                .ToList();
        }

        private static List<SearchResultDto> FuseWithWeightedSum(
            IReadOnlyList<ChunkSearchResult> denseResults,
            IReadOnlyList<ChunkFullTextResult> sparseResults,
            int topK,
            double alpha = 0.5)
        {
            var denseRaw = denseResults.ToDictionary(r => r.ChunkId, r => 1 - r.Distance);
            var sparseRaw = sparseResults.ToDictionary(r => r.ChunkId, r => r.Rank);

            var denseNorm = MinMaxNormalize(denseRaw);
            var sparseNorm = MinMaxNormalize(sparseRaw);

            var metadata = new Dictionary<Guid, ChunkMetadata>();
            foreach (var r in denseResults) metadata.TryAdd(r.ChunkId, new ChunkMetadata(r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading));
            foreach (var r in sparseResults) metadata.TryAdd(r.ChunkId, new ChunkMetadata(r.DocumentId, r.DocumentTitle, r.Text, r.PageNumber, r.SectionHeading));

            var allIds = denseRaw.Keys.Union(sparseRaw.Keys);

            return allIds
                .Select(id => (Id: id, Score: alpha * denseNorm.GetValueOrDefault(id, 0) + (1 - alpha) * sparseNorm.GetValueOrDefault(id, 0)))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => ToDto(x.Id, x.Score, metadata[x.Id]))
                .ToList();
        }

        private static Dictionary<Guid, double> MinMaxNormalize(Dictionary<Guid, double> raw)
        {
            if (raw.Count == 0) return new Dictionary<Guid, double>();

            var min = raw.Values.Min();
            var max = raw.Values.Max();

            if (Math.Abs(max - min) < 1e-9)
                return raw.ToDictionary(kv => kv.Key, _ => 1.0);

            return raw.ToDictionary(kv => kv.Key, kv => (kv.Value - min) / (max - min));
        }

        private static SearchResultDto ToDto(Guid chunkId, double score, ChunkMetadata m) =>
            new(chunkId, m.DocumentId, m.DocumentTitle, m.Text, m.PageNumber, m.SectionHeading, score);

        private record ChunkMetadata(Guid DocumentId, string DocumentTitle, string Text, int? PageNumber, string? SectionHeading);
    }
}
