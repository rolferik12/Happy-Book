namespace Happy.Document.Word
{
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using HtmlToOpenXml;

    public class WordWriter : IWriter
    {
        private WordprocessingDocument _document;

        public WordWriter(string url)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Template\Template.docx");

            var filePath = $"{url}.docx";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Copy(templatePath, filePath);

            _document = WordprocessingDocument.Open(filePath, true);
        }

        public void Save()
        {
            _document.Save();
            _document.Dispose();
        }
        public void WriteChapterFromHtml(string title, string html)

        {
            try
            {


                var body = _document.MainDocumentPart?.Document.Body ?? null;

                if (body == null) { throw new MissingFieldException(nameof(body)); }

                WriteHeader1(title, body);

                HtmlConverter converter = new HtmlConverter(_document.MainDocumentPart);
                converter.ParseHtml(html);

                //Apply page break
                var para = body.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run());
                run.AppendChild(new Break { Type = BreakValues.Page });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());   
            }
        }

        private void WriteHeader1(string header, Body wordBody)
        {
            var para = wordBody.AppendChild(new Paragraph());

            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(header));
            para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" });
        }
    }
}
