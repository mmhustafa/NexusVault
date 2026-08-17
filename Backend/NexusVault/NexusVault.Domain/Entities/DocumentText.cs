using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Domain.Entities
{
    public class DocumentText
    {
        public Guid DocumentVersionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ExtractionMethod { get; set; } = string.Empty; // e.g. "PdfPig", "OpenXml"
        public int? PageCount { get; set; }
        public DateTimeOffset ExtractedAt { get; set; }

        public DocumentVersion? DocumentVersion { get; set; }
    }
}
