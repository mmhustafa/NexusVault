using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class Chunk
    {
        public Guid Id { get; set; }
        public Guid DocumentVersionId { get; set; }
        public Guid TenantId { get; set; }
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string? SectionHeading { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        public DocumentVersion? DocumentVersion { get; set; }
        public Embedding? Embedding { get; set; }
    }
}
