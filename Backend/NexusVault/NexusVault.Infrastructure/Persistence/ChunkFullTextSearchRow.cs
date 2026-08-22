using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence
{
    public class ChunkFullTextSearchRow
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string? SectionHeading { get; set; }
        public double Rank { get; set; }
    }
}
