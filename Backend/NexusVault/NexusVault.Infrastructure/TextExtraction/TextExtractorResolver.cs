using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.TextExtraction
{
    public class TextExtractorResolver
    {
        private readonly IEnumerable<ITextExtractor> _extractors;

        public TextExtractorResolver(IEnumerable<ITextExtractor> extractors)
        {
            _extractors = extractors;
        }

        public ITextExtractor Resolve(string contentType)
        {
            var extractor = _extractors.FirstOrDefault(e => e.CanHandle(contentType));
            if (extractor is null)
                throw new NotSupportedException($"No text extractor registered for content type '{contentType}'.");

            return extractor;
        }
    }
}
