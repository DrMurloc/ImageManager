using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ImageManager.Application;

namespace ImageManager.Infrastructure;

public sealed class OpenXmlDocxTextExtractor : IDocxTextExtractor
{
    public string Extract(byte[] docx)
    {
        using var stream = new MemoryStream(docx);
        using var document = WordprocessingDocument.Open(stream, false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null) return "";

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            if (text.Length > 0) sb.AppendLine(text);
        }

        return sb.ToString();
    }
}
