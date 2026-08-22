using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IChunkSearchRepository
    {
        Task<IReadOnlyList<ChunkSearchResult>> FindNearestAsync(
            float[] queryVector,
            Guid tenantId,
            int topK,
            Guid? documentId = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<ChunkFullTextResult>> FindByFullTextAsync(
            string queryText,
            Guid tenantId,
            int topK,
            Guid? documentId = null,
            CancellationToken ct = default);
    }

}
