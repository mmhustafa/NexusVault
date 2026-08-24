using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record RagSourceChunk(Guid Id, string Text);
    public record RagSynthesisResult(string Answer, IReadOnlyList<Guid> CitedChunkIds);

}
