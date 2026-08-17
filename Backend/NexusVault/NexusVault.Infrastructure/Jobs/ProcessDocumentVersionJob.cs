using Hangfire;
using Microsoft.Extensions.Logging;
using NexusVault.Application.Interfaces;
using NexusVault.Domain.Entities;
using NexusVault.Domain.Enums;
using NexusVault.Infrastructure.Persistence;
using NexusVault.Infrastructure.TextExtraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Jobs
{
    /// <summary>
    /// Phase 1's entire background pipeline: Pending -> Processing -> extract
    /// text -> Ready (or Failed). This is deliberately the ONLY step for now --
    /// Phase 2 extends this same job (or chains a new one after it) with
    /// chunking and embedding, rather than this being rewritten wholesale.
    ///
    /// Retry policy: [AutomaticRetry] is Hangfire's built-in exponential-backoff
    /// retry. attemptsMax is intentionally modest (3) -- extraction failures are
    /// usually deterministic (corrupt file, unsupported encoding), so hammering
    /// retries rarely helps; what matters is that a transient failure (e.g. a
    /// momentary disk I/O issue) gets a couple of chances before landing in
    /// Failed for manual inspection.
    /// </summary>

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 60, 300 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class ProcessDocumentVersionJob
    {
        private readonly NexusVaultDbContext _db;
        private readonly IFileStorage _fileStorage;
        private readonly TextExtractorResolver _extractorResolver;
        private readonly ILogger<ProcessDocumentVersionJob> _logger;

        public ProcessDocumentVersionJob(
            NexusVaultDbContext db,
            IFileStorage fileStorage,
            TextExtractorResolver extractorResolver,
            ILogger<ProcessDocumentVersionJob> logger)
        {
            _db = db;
            _fileStorage = fileStorage;
            _extractorResolver = extractorResolver;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid documentVersionId, string correlationId, CancellationToken ct)
        {
            // correlationId flows into every log line for this run -- this is
            // what lets you reconstruct one document's full processing history
            // from logs alone, and it's the same id that will be forwarded as a
            // header to FastAPI once Phase 2 adds calls out to the AI service.
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

            var version = await _db.DocumentVersions.FindAsync(new object?[] { documentVersionId }, ct);
            if (version is null)
            {
                _logger.LogWarning("DocumentVersion {VersionId} not found -- skipping (likely deleted before job ran)", documentVersionId);
                return;
            }

            // Idempotency guard: if this exact version already has extracted
            // text and is already Ready, a retried/duplicate job run should be a
            // safe no-op rather than redoing work or throwing.
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
                await using var fileStream = await _fileStorage.OpenReadAsync(version.StoragePath, ct);
                var extractor = _extractorResolver.Resolve(version.ContentType);
                var result = await extractor.ExtractAsync(fileStream, ct);

                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    // Not an exception -- a PDF with no text layer (scanned
                    // images) is a valid, expected outcome, not a bug. Flag it
                    // as Failed with a clear reason rather than silently
                    // "succeeding" with an empty, unsearchable document.
                    throw new InvalidOperationException(
                        "Extraction produced no text. The file may be a scanned/image-only document -- OCR is not yet supported.");
                }

                var existingText = await _db.DocumentTexts.FindAsync(new object?[] { documentVersionId }, ct);
                if (existingText is null)
                {
                    _db.DocumentTexts.Add(new DocumentText
                    {
                        DocumentVersionId = documentVersionId,
                        Content = result.Text,
                        ExtractionMethod = result.Method,
                        PageCount = result.PageCount,
                        ExtractedAt = DateTimeOffset.UtcNow
                    });
                }
                else
                {
                    existingText.Content = result.Text;
                    existingText.ExtractionMethod = result.Method;
                    existingText.PageCount = result.PageCount;
                    existingText.ExtractedAt = DateTimeOffset.UtcNow;
                }

                version.Status = DocumentVersionStatus.Ready;
                version.ReadyAt = DateTimeOffset.UtcNow;
                version.ErrorMessage = null;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "DocumentVersion {VersionId} extracted successfully via {Method} ({CharCount} chars)",
                    documentVersionId, result.Method, result.Text.Length);
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
