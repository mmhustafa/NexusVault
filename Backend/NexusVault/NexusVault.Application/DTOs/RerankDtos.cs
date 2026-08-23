using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record RerankCandidate(Guid Id, string Text);
    public record RerankedResult(Guid Id, double Score);
}
