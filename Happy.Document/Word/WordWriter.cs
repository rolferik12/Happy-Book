namespace Happy.Document.Word
{
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using DocumentFormat.OpenXml;
    using Happy.Reader;
    using HtmlToOpenXml;
    using System.Text.RegularExpressions;

    public class WordWriter : IWriter
    {
        private static int CHAPTERS_PER_FILE = 200;
        private List<WordprocessingDocument> _documents = new List<WordprocessingDocument>();
        private WordprocessingDocument _document;
        private int _counter = 0;
        private readonly string _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Template\Template.docx");

        private string _folderPath = string.Empty;
        private string _name = string.Empty;

        public List<string> FailedChapters { get; private set; } = new List<string>();
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

        private async Task<string> ValidateAndCleanHtmlAsync(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            var imgTagPattern = @"<img[^>]+src\s*=\s*[""']([^""']+)[""'][^>]*>";
            var matches = Regex.Matches(html, imgTagPattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return html;

            var invalidImages = new List<string>();
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            foreach (Match match in matches)
            {
                var imageUrl = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);

                try
                {
                    using var response = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
                    {
                        invalidImages.Add(match.Value);
                        continue;
                    }

                    using var stream = await response.Content.ReadAsStreamAsync();
                    if (stream.Length == 0 || !stream.CanRead)
                    {
                        invalidImages.Add(match.Value);
                    }
                }
                catch
                {
                    invalidImages.Add(match.Value);
                }
            }

            var cleanedHtml = html;
            foreach (var invalidImg in invalidImages)
            {
                cleanedHtml = cleanedHtml.Replace(invalidImg, "<!-- Image removed: validation failed -->");
            }

            return cleanedHtml;
        }

        public async Task WriteChapterAsync(Chapter chapter)
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

                // Validate and clean HTML to remove broken images
                var cleanedHtml = await ValidateAndCleanHtmlAsync(chapter.Html);

                HtmlConverter converter = new HtmlConverter(_document.MainDocumentPart);
                var paragraphs = converter.Parse(cleanedHtml);
                foreach (var paragraph in paragraphs)
                {
                    body.Append(paragraph);
                }

                //Apply page break
                var para = body.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run());
                run.AppendChild(new Break { Type = BreakValues.Page });
                _counter++;
            }
            catch (Exception ex)
            {
                FailedChapters.Add(chapter.Title);
                Console.WriteLine($"Failed to write chapter '{chapter.Title}': {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
