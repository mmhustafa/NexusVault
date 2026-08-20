using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.DTOs
{
    public record EmbedResult
    (
        IReadOnlyList<float[]> Vectors,
        string ModelName,
        string ModelVersion,
        int Dimensions
    );
}
