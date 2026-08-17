using NexusVault.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class DocumentVersion
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid TenantId { get; set; }

        public int VersionNumber { get; set; }
        public bool IsCurrent { get; set; } = true;

        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        /// <summary>Local filesystem path for Phase 1. Swappable for blob storage later
        /// behind IFileStorage without touching this entity.</summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>SHA-256 of the raw uploaded file bytes. Unique per (DocumentId),
        /// enforced at the DB level -- see EF configuration.</summary>
        public string ContentHash { get; set; } = string.Empty;

        public DocumentVersionStatus Status { get; set; } = DocumentVersionStatus.Pending;
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ProcessingStartedAt { get; set; }
        public DateTimeOffset? ReadyAt { get; set; }

        public Document? Document { get; set; }
        public DocumentText? Text { get; set; }
    }
}
