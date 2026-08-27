using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using NexusVault.Domain.Entities;
using NexusVault.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Services
{
    public class DocumentIngestionService
    {
        private static readonly string[] AllowedContentTypes =
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" // .docx
        };

        private const long MaxFileSizeBytes = 25 * 1024 * 1024;

        private readonly IDocumentRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly IIngestionJobScheduler _jobScheduler;

        public DocumentIngestionService(

            IDocumentRepository repository,
            IFileStorage fileStorage,
            IIngestionJobScheduler jobScheduler)
        {
            _repository = repository;
            _fileStorage = fileStorage;
            _jobScheduler = jobScheduler;
        }

        public async Task<UploadDocumentResult> UploadAsync(
            Guid tenantId,
            Guid userId,
            string title,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            Stream fileStream,
            CancellationToken ct = default)
        {
            if (!AllowedContentTypes.Contains(contentType))
                throw new InvalidOperationException($"Unsupported content type '{contentType}'. Allowed: PDF, DOCX.");

            if (fileSizeBytes <= 0 || fileSizeBytes > MaxFileSizeBytes)
                throw new InvalidOperationException($"File size must be between 1 byte and {MaxFileSizeBytes} bytes.");

            var contentHash = await ContentHasher.ComputeSha256Async(fileStream, ct);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = title,
                CreatedBy = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var existing = await _repository.FindByContentHashAsync(document.Id, contentHash, ct);
            // Note: for a brand-new Document this will always be null (no prior
            // versions exist yet) -- the real duplicate-detection value of this
            // check shows up once Phase 9 adds "new version of an existing
            // document" as an explicit operation. Kept here now so the pattern
            // and the repository method exist before they're load-bearing.

            var storagePath = await _fileStorage.SaveAsync(document.Id, originalFileName, fileStream, ct);

            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = tenantId,
                VersionNumber = 1,
                IsCurrent = true,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
                StoragePath = storagePath,
                ContentHash = contentHash,
                Status = DocumentVersionStatus.Pending,
                AttemptCount = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };

            document.CurrentVersionId = version.Id;

            await _repository.AddDocumentAsync(document, ct);
            await _repository.AddVersionAsync(version, ct);
            await _repository.SaveChangesAsync(ct);

            var correlationId = Guid.NewGuid().ToString("N");
            _jobScheduler.EnqueueProcessDocumentVersion(version.Id, correlationId);

            return new UploadDocumentResult(document.Id, version.Id, version.Status.ToString(), WasDuplicate: false);
        }
        public async Task<DocumentVersionStatusResult?> GetStatusAsync(Guid documentVersionId,Guid tenantId, CancellationToken ct = default)
        {
            var version = await _repository.GetVersionAsync(documentVersionId, ct);

            if (version is null || version.TenantId != tenantId) return null;

            return new DocumentVersionStatusResult(
                version.Id,
                version.Status.ToString(),
                version.AttemptCount,
                version.ErrorMessage,
                version.CreatedAt,
                version.ReadyAt);
        }
    }
}
