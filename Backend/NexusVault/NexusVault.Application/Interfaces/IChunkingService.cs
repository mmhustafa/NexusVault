using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IChunkingService
    {
        Task<IReadOnlyList<ChunkDto>> ChunkAsync(
        string text,
        int maxTokensPerChunk = 300,
        CancellationToken ct = default);
    }
}
