using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record AskRequest(string Query, Guid? DocumentId = null);

    public record CitationDto(
        Guid ChunkId,
        Guid DocumentId,
        string DocumentTitle,
        int? PageNumber,
        string? SectionHeading
    );

    public record AskResultDto(string Answer, IReadOnlyList<CitationDto> Citations);
}
