namespace Happy.Document.Word
{
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Happy.Reader;
    using HtmlToOpenXml;

    public class WordWriter : IWriter
    {
        private static int CHAPTERS_PER_FILE = 200;
        private List<WordprocessingDocument> _documents = new List<WordprocessingDocument>();
        private WordprocessingDocument _document;
        private int _counter = 0;
        private readonly string _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Template\Template.docx");

        private string _folderPath = string.Empty;
        private string _name = string.Empty;
        public WordWriter(string name, string folderPath)
        {
            _folderPath = folderPath + " docx";
            _name = name;

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            //Sets initial document
            SetDocument(_folderPath, name);
        }

        private void SetDocument(string folderPath, string name)
        {
            var filePath = $"{folderPath}\\{name}.docx";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Copy(_templatePath, filePath);

            _document = WordprocessingDocument.Open(filePath, true);
        }

        public void Save()
        {
            _documents.Add(_document);

            foreach (WordprocessingDocument document in _documents)
            {
                document.Save();
                document.Dispose();
            }
            
        }
        public void WriteChapterFromHtml(string title, string html)
        {
            
        }

        private void WriteHeader1(string header, Body wordBody)
        {
            var para = wordBody.AppendChild(new Paragraph());

            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(header));
            para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" });
        }

        public void WriteChapter(Chapter chapter)
        {
            if (_counter >= CHAPTERS_PER_FILE)
            {
                _documents.Add(_document);
                SetDocument(_folderPath, $"{_name}{_documents.Count}");
                _counter = 0;
            }
            try
            {
                var body = _document.MainDocumentPart?.Document.Body ?? null;

                if (body == null) { throw new MissingFieldException(nameof(body)); }

                WriteHeader1(chapter.Title, body);

                HtmlConverter converter = new HtmlConverter(_document.MainDocumentPart);
                converter.ParseHtml(chapter.Html);

                //Apply page break
                var para = body.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run());
                run.AppendChild(new Break { Type = BreakValues.Page });
                _counter++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
