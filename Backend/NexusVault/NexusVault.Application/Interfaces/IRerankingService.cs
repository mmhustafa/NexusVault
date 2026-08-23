using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    
    public interface IRerankingService
    {
        Task<IReadOnlyList<RerankedResult>> RerankAsync(
        string query,
        IReadOnlyList<RerankCandidate> candidates,
        CancellationToken ct = default);
    }
}
