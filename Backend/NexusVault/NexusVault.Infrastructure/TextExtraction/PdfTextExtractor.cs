using DocumentFormat.OpenXml.Spreadsheet;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;

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
            var pageIndex = 0;

            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                pageIndex++;

                var words = page.GetWords().ToList();
                if (words.Count == 0)
                {
                    if (pageIndex < pageCount) sb.Append('\f');
                    continue;
                }

                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
                var orderedBlocks = UnsupervisedReadingOrderDetector.Instance.Get(blocks)
                    .OrderBy(b => b.ReadingOrder);

                foreach (var block in orderedBlocks)
                {
                    ct.ThrowIfCancellationRequested();
                    var blockText = block.Text.Trim();
                    if (string.IsNullOrWhiteSpace(blockText)) continue;

                    sb.AppendLine(blockText);
                    sb.AppendLine(); // explicit paragraph boundary -- the chunker
                                     // splits on this blank line, same convention
                                     // DocxTextExtractor now uses too.
                }

                if (pageIndex < pageCount)
                    sb.Append('\f'); 
            }

            return Task.FromResult(new ExtractionResult(sb.ToString().Trim(), "PdfPig", pageCount));
        }
    }
}
