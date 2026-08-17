using Microsoft.EntityFrameworkCore;
using NexusVault.Application.Interfaces;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Repositories
{
    public class DocumentRepository: IDocumentRepository
    {
        private readonly NexusVaultDbContext _db;

        public DocumentRepository(NexusVaultDbContext db)
        {
            _db = db;
        }

        public Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken ct = default) =>
            _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);

        public Task<DocumentVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default) =>
            _db.DocumentVersions.Include(v => v.Text).FirstOrDefaultAsync(v => v.Id == versionId, ct);

        public Task<DocumentVersion?> FindByContentHashAsync(Guid documentId, string contentHash, CancellationToken ct = default) =>
            _db.DocumentVersions.FirstOrDefaultAsync(
                v => v.DocumentId == documentId && v.ContentHash == contentHash, ct);

        public async Task AddDocumentAsync(Document document, CancellationToken ct = default) =>
            await _db.Documents.AddAsync(document, ct);

        public async Task AddVersionAsync(DocumentVersion version, CancellationToken ct = default) =>
            await _db.DocumentVersions.AddAsync(version, ct);

        public async Task SaveTextAsync(DocumentText text, CancellationToken ct = default)
        {
            var existing = await _db.DocumentTexts.FirstOrDefaultAsync(
                t => t.DocumentVersionId == text.DocumentVersionId, ct);

            if (existing is null)
            {
                await _db.DocumentTexts.AddAsync(text, ct);
            }
            else
            {
                // Idempotency at the write level: if this job runs twice (retry
                // after a crash between "text saved" and "status flipped to
                // Ready"), we overwrite rather than insert a duplicate row --
                // DocumentVersionId is the primary key, so this is a natural fit
                // rather than something we had to special-case.
                existing.Content = text.Content;
                existing.ExtractionMethod = text.ExtractionMethod;
                existing.PageCount = text.PageCount;
                existing.ExtractedAt = text.ExtractedAt;
            }
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
