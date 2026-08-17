using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Application.Interfaces
{
    public interface ITextExtractor 
    {
        /// <summary>Whether this extractor can handle the given content type.</summary>
        bool CanHandle(string contentType);

        Task<ExtractionResult> ExtractAsync(Stream fileStream, CancellationToken ct = default);
    }

    public record ExtractionResult(string Text, string Method, int? PageCount);
}
