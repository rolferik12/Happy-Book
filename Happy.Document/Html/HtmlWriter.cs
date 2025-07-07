namespace Happy.Document.Html
{
    using System;

    public class HtmlWriter : IWriter
    {
        private static int CHAPTERS_PER_FILE = 10;
        private List<HtmlDocument> _documents = new List<HtmlDocument>();
        private string _currentDocument = string.Empty;
        private string _storagePath = string.Empty;
        private string _name = string.Empty;
        private int _counter = 0;
        public HtmlWriter(string name, string path)
        {
            _storagePath = path;
            _name = name;
            
        }
        public void Save()
        {
            if (!string.IsNullOrEmpty(_currentDocument))
            {
                _documents.Add(new HtmlDocument(_currentDocument));
            }

            var multipleFiles = _documents.Count > 1;

            if (multipleFiles)
            {
                if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
                _storagePath += $"\\{_name}";
            }

            for (int i = 0; i < _documents.Count; i++)
            {
                var currentDoc = _documents[i];
                var filePath = _storagePath + (multipleFiles ? i + 1 : "");
                File.WriteAllText($"{filePath}.html", currentDoc.GetHtmlFileContent());
            }
        }

        public void WriteChapterFromHtml(string title, string html)
        {
  
            _currentDocument += $"<h1>{title}</h1><br />{html}";
            _counter++;

            if (_counter == CHAPTERS_PER_FILE)
            {
                
                _documents.Add(new HtmlDocument(_currentDocument));
                _currentDocument = string.Empty;
                _counter = 0;
            }
        }
    }
}
