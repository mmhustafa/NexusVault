using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NexusVault.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.TextExtraction
{

    public class DocxTextExtractor : ITextExtractor
    {
        public bool CanHandle(string contentType) =>
            contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        public Task<ExtractionResult> ExtractAsync(Stream fileStream, CancellationToken ct = default)
        {
            fileStream.Position = 0;

            var sb = new StringBuilder();
            using var wordDoc = WordprocessingDocument.Open(fileStream, false);

            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body is not null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    ct.ThrowIfCancellationRequested();
                    var text = paragraph.InnerText;

                    if (string.IsNullOrWhiteSpace(text)) continue;

                    sb.AppendLine(text.Trim());
                    sb.AppendLine(); // explicit paragraph boundary -- matches PdfTextExtractor's convention
                }
            }

            // DOCX has no intrinsic "page" concept at the XML level (pagination
            // is a rendering-time computation) -- PageCount is intentionally null
            // here rather than a fabricated value.
            return Task.FromResult(new ExtractionResult(sb.ToString().Trim(), "OpenXml", PageCount: null));
        }
    }
}
