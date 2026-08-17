using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;

namespace NexusVault.Infrastructure.TextExtraction
{
    
    public class PdfTextExtractor : ITextExtractor
    {
        public bool CanHandle(string contentType) => contentType == "application/pdf";

        public Task<ExtractionResult> ExtractAsync(Stream fileStream, CancellationToken ct = default)
        {
            fileStream.Position = 0;

            var sb = new StringBuilder();
            using var document = PdfDocument.Open(fileStream);

            var pageCount = document.NumberOfPages;
            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                sb.AppendLine(page.Text);
                sb.AppendLine(); // page-boundary marker; useful signal for Phase 2 chunking
            }

            return Task.FromResult(new ExtractionResult(sb.ToString().Trim(), "PdfPig", pageCount));
        }
    }
}
