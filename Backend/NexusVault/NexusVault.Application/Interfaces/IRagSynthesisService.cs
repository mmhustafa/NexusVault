using NexusVault.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface IRagSynthesisService
    {
        Task<RagSynthesisResult> SynthesizeAsync(
            string query,
            IReadOnlyList<RagSourceChunk> chunks,
            CancellationToken ct = default);
    }
}
