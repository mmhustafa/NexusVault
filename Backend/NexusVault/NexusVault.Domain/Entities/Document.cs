using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public Guid? CurrentVersionId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }
}
