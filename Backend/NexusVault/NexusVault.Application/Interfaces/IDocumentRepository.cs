using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Domain.Entities.Document?> GetDocumentAsync(Guid documentId, CancellationToken ct = default);

        Task<DocumentVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default);

        /// <summary>Finds an existing version for this document with the same content
        /// hash, regardless of status. This is the idempotency check: if a match
        /// exists, the caller should not create a new version or re-enqueue processing.</summary>
        Task<DocumentVersion?> FindByContentHashAsync(Guid documentId, string contentHash, CancellationToken ct = default);

        Task<int> GetNextVersionNumberAsync(Guid documentId, CancellationToken ct = default);

        Task<IReadOnlyList<DocumentVersion>> GetVersionsForDocumentAsync(Guid documentId, CancellationToken ct = default);

        Task AddDocumentAsync(Domain.Entities.Document document, CancellationToken ct = default);

        Task AddVersionAsync(DocumentVersion version, CancellationToken ct = default);

        Task SaveTextAsync(DocumentText text, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
