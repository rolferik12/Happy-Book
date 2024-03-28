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
            converter.ParseHtml(chapter.html);

            //Apply page break
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Break { Type = BreakValues.Page });
        }

        private void WriteHeader(HappyText header, Body wordBody)
        {
            var para = wordBody.AppendChild(new Paragraph());

            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(header.Value));
            para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" });
        }

        private Paragraph? WriteParagraph(HappyParagraph paragraph)
        {
            var wordParagraph = new Paragraph();

            foreach (var text in paragraph.Texts)
            {
                if (text.Value == "&nbsp;") continue;
                if (paragraph.Texts.Count == 1 && string.IsNullOrWhiteSpace(text.Value.Replace("\n", ""))) continue;

                var run = wordParagraph.AppendChild(new Run());

                var props = run.AppendChild(new RunProperties());


                if (text.Bold) props.Append(new Bold { Val = OnOffValue.FromBoolean(true) });
                if (text.Cursive) props.Append(new Italic { Val = OnOffValue.FromBoolean(true) });

                var textBreaks = text.Value.Split("\n", StringSplitOptions.RemoveEmptyEntries);

                if (textBreaks.Length > 1)
                {
                    for (int i = 0; i < textBreaks.Length; i++)
                    {
                        var textBreak = textBreaks[i];
                        if (string.IsNullOrWhiteSpace(textBreak)) continue;
                        if (i > 0)
                            run.AppendChild(new Break());

                        run.AppendChild(new Text(textBreak) { Space = SpaceProcessingModeValues.Preserve });
                    }
                }
                else if (text.Value.Contains("\n"))
                    run.AppendChild(new Break());
                else run.AppendChild(new Text(text.Value) { Space = SpaceProcessingModeValues.Preserve });
            }
            if (wordParagraph.ChildElements.Count > 0)
                return wordParagraph;

            return null;
        }
    }
}
