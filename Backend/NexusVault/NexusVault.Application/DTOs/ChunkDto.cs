using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record ChunkDto
    (
        int ChunkIndex,
        string Text,
        int? PageNumber,
        string? SectionHeading
    );
}
