using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record ChunkSearchResult
    (
        Guid ChunkId,
        Guid DocumentId,
        string DocumentTitle,
        string Text,
        int? PageNumber,
        string? SectionHeading,
        double Distance
    );
}
