using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<EmbedResult> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
    }
}
