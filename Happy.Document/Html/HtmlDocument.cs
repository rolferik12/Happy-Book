namespace Happy.Document.Html
{
    public class HtmlDocument
    {
        public HtmlDocument(string text)
        {
            Text = text;
        }
        public bool IsDone { get; set; }
        public string Beginning => "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"></head><body>";
        public string End => "</body></html>";

        public string Text { get; set; } = string.Empty;

        public string GetHtmlFileContent()
        {
            return $"{Beginning}{Text}{End}";
        }
    }
}
