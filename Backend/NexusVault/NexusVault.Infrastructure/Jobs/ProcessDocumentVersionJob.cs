using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;
using NexusVault.Domain.Entities;
using NexusVault.Domain.Enums;
using NexusVault.Infrastructure.Persistence;
using NexusVault.Infrastructure.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Jobs
{

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 60, 300 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class ProcessDocumentVersionJob
    {
        private readonly NexusVaultDbContext _db;
        private readonly IFileStorage _fileStorage;
        private readonly TextExtractorResolver _extractorResolver;
        private readonly IChunkingService _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<ProcessDocumentVersionJob> _logger;

        public ProcessDocumentVersionJob(
            NexusVaultDbContext db,
            IFileStorage fileStorage,
            TextExtractorResolver extractorResolver,
            IChunkingService chunkingService,
            IEmbeddingService embeddingService,
            ILogger<ProcessDocumentVersionJob> logger)
        {
            _db = db;
            _fileStorage = fileStorage;
            _extractorResolver = extractorResolver;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid documentVersionId, string correlationId, CancellationToken ct)
        {

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["DocumentVersionId"] = documentVersionId
            });

            var version = await _db.DocumentVersions.FindAsync(new object?[] { documentVersionId }, ct);
            if (version is null)
            {
                _logger.LogWarning("DocumentVersion {VersionId} not found -- skipping", documentVersionId);
                return;
            }

            // Idempotency guard: already fully processed, skip safely
            var alreadyProcessed = await _db.DocumentTexts.FindAsync(new object?[] { documentVersionId }, ct);
            if (alreadyProcessed is not null && version.Status == DocumentVersionStatus.Ready)
            {
                _logger.LogInformation("DocumentVersion {VersionId} already processed -- skipping", documentVersionId);
                return;
            }

            version.Status = DocumentVersionStatus.Processing;
            version.ProcessingStartedAt = DateTimeOffset.UtcNow;
            version.AttemptCount += 1;
            await _db.SaveChangesAsync(ct);

            try
            {
                // Step 1: Text extraction
                await using var fileStream = await _fileStorage.OpenReadAsync(version.StoragePath, ct);
                var extractor = _extractorResolver.Resolve(version.ContentType);
                var extractionResult = await extractor.ExtractAsync(fileStream, ct);

                if (string.IsNullOrWhiteSpace(extractionResult.Text))
                    throw new InvalidOperationException(
                        "Extraction produced no text. The file may be a scanned/image-only document -- OCR is not yet supported.");

                var existingText = await _db.DocumentTexts.FindAsync(new object?[] { documentVersionId }, ct);
                if (existingText is null)
                {
                    _db.DocumentTexts.Add(new DocumentText
                    {
                        DocumentVersionId = documentVersionId,
                        Content = extractionResult.Text,
                        ExtractionMethod = extractionResult.Method,
                        PageCount = extractionResult.PageCount,
                        ExtractedAt = DateTimeOffset.UtcNow
                    });
                }
                else
                {
                    existingText.Content = extractionResult.Text;
                    existingText.ExtractionMethod = extractionResult.Method;
                    existingText.PageCount = extractionResult.PageCount;
                    existingText.ExtractedAt = DateTimeOffset.UtcNow;
                }

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Extraction complete: {CharCount} chars via {Method}",
                    extractionResult.Text.Length, extractionResult.Method);

                // Step 2: Chunking
                var chunks = await _chunkingService.ChunkAsync(extractionResult.Text, ct: ct);
                _logger.LogInformation("Chunking complete: {ChunkCount} chunks", chunks.Count);

                // Step 3: Embedding
                var chunkTexts = chunks.Select(c => c.Text).ToList();
                var embedResult = await _embeddingService.EmbedAsync(chunkTexts, ct);
                _logger.LogInformation("Embedding complete: {Dims}-dim vectors via {Model}",
                    embedResult.Dimensions, embedResult.ModelName);

                // Step 4: Persist chunks + embeddings
                // Remove existing chunks for idempotency on retry -- safe because
                // the unique index on (DocumentVersionId, ChunkIndex) would reject
                var existingChunks = _db.Chunks.Where(c => c.DocumentVersionId == documentVersionId);
                _db.Chunks.RemoveRange(existingChunks);
                await _db.SaveChangesAsync(ct);

                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    var vector = embedResult.Vectors[i];

                    var chunkEntity = new Chunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentVersionId = documentVersionId,
                        TenantId = version.TenantId,
                        ChunkIndex = chunk.ChunkIndex,
                        Text = chunk.Text,
                        PageNumber = chunk.PageNumber,
                        SectionHeading = chunk.SectionHeading,
                        ContentHash = ContentHasher.ComputeSha256(chunk.Text),
                        CreatedAt = DateTimeOffset.UtcNow,
                        Embedding = new Embedding
                        {
                            Vector = vector,
                            ModelName = embedResult.ModelName,
                            ModelVersion = embedResult.ModelVersion,
                            Dimensions = embedResult.Dimensions,
                            CreatedAt = DateTimeOffset.UtcNow
                        }
                    };

                    _db.Chunks.Add(chunkEntity);
                }

                await _db.SaveChangesAsync(ct);

                // Step 5: Mark ready
                version.Status = DocumentVersionStatus.Ready;
                version.ReadyAt = DateTimeOffset.UtcNow;
                version.ErrorMessage = null;

                // Step 6: Archival swap
                if (!version.IsCurrent)
                {
                    var previouslyCurrent = await _db.DocumentVersions
                        .Where(v => v.DocumentId == version.DocumentId && v.IsCurrent && v.Id != version.Id)
                        .ToListAsync(ct);

                    foreach (var old in previouslyCurrent)
                        old.IsCurrent = false;

                    version.IsCurrent = true;

                    var document = await _db.Documents.FindAsync(new object?[] { version.DocumentId }, ct);
                    if (document is not null)
                        document.CurrentVersionId = version.Id;

                    _logger.LogInformation(
                        "DocumentVersion {VersionId} activated as current; archived {ArchivedCount} previous version(s)",
                        documentVersionId, previouslyCurrent.Count);
                }

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("DocumentVersion {VersionId} fully processed: {ChunkCount} chunks indexed",
                    documentVersionId, chunks.Count);

            }
            catch (Exception ex)
            {
                version.Status = DocumentVersionStatus.Failed;
                version.ErrorMessage = ex.Message;
                await _db.SaveChangesAsync(ct);

                _logger.LogError(ex, "DocumentVersion {VersionId} extraction failed on attempt {Attempt}",
                    documentVersionId, version.AttemptCount);

                throw; // rethrow so Hangfire's [AutomaticRetry] actually retries it
            }
        }
    }
}
