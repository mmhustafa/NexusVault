using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public enum SearchMode
    {
        Dense = 0,   // default 
        Sparse = 1,
        Hybrid = 2
    }

    public enum FusionStrategy
    {
        Rrf = 0,
        WeightedSum = 1
    }

    public record SearchRequest(
        string Query,
        int? TopK = null,
        Guid? DocumentId = null,
        SearchMode Mode = SearchMode.Dense,
        FusionStrategy Fusion = FusionStrategy.Rrf
    );

    public record SearchResultDto(
        Guid ChunkId,
        Guid DocumentId,
        string DocumentTitle,
        string Text,
        int? PageNumber,
        string? SectionHeading,
        double Score
    );


}
