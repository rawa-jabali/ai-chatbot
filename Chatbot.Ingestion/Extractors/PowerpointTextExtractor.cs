using System.Text;
using ChatBot.Core.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace ChatBot.Ingestion.Extractors;

public class PowerPointPptxExtractor : ITextExtractor
{
    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName).Equals(".pptx", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractResult> ExtractAsync(Stream file, string fileName, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        ms.Position = 0;

        using var pres = PresentationDocument.Open(ms, false);
        var presentationPart = pres.PresentationPart;
        if (presentationPart?.Presentation is null)
            return Task.FromResult(new ExtractResult(fileName, Array.Empty<ExtractSection>()));

        var sections = new List<ExtractSection>();

        var slideIdList = presentationPart.Presentation.SlideIdList?.ChildElements;
        if (slideIdList is null)
            return Task.FromResult(new ExtractResult(fileName, Array.Empty<ExtractSection>()));

        int slideNumber = 0;

        foreach (SlideId slideId in slideIdList.OfType<SlideId>())
        {
            slideNumber++;

            var relId = slideId.RelationshipId;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId!);

            var sb = new StringBuilder();

            foreach (var text in slidePart.Slide.Descendants<A.Text>())
            {
                var t = text.Text?.Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                sb.AppendLine(t);
            }

            var slideText = sb.ToString().Trim();
            if (slideText.Length == 0) continue;

            sections.Add(new ExtractSection(slideText, new Dictionary<string,string>
            {
                ["type"] = "pptx",
                ["slide"] = slideNumber.ToString()
            }));
        }

        return Task.FromResult(new ExtractResult(fileName, sections));
    }
}