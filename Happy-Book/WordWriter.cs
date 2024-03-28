using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;

namespace Happy_Book
{
    internal class WordWriter
    {
        private WordprocessingDocument _document;

        public WordWriter(string url)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Template\Template.docx");

            if (File.Exists(url))
            {
                File.Delete(url);
            }

            File.Copy(templatePath, url);

            _document = WordprocessingDocument.Open(url, true);
        }

        public void Save()
        {
            _document.Save();
            _document.Dispose();
        }
        public void WriteChapter(Chapter chapter)
        {
            var body = _document.MainDocumentPart?.Document.Body ?? null;

            if (body == null) { throw new MissingFieldException(nameof(body)); }

            WriteHeader(chapter.Title, body);

            HtmlConverter converter = new HtmlConverter(_document.MainDocumentPart);
            converter.ParseHtml(chapter.Html);

            //Apply page break
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Break { Type = BreakValues.Page });
        }

        private void WriteHeader(string header, Body wordBody)
        {
            var para = wordBody.AppendChild(new Paragraph());

            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(header));
            para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" });
        }
    }
}
