namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Reflection.Metadata;
    using System.Web;

    public class NovelFireReader : BaseReader
    {

        public override string Domain { get; } = "";

        public NovelFireReader(string url, string bookName, string headerToRemove) : base(url, bookName, headerToRemove)
        {
        }

        public override string GetChapterHtml(HtmlDocument document)
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@id='content']");

            if (chapterNode == null) return string.Empty;

            ChangeTableWidth(chapterNode, 100);

            return chapterNode.InnerHtml;
        }

        public override string GetNextChapterLink(HtmlDocument document)
        {
            var nextButton = document.DocumentNode.SelectSingleNode("//a[contains(@class, 'nextchap')]");

            if (nextButton == null) return string.Empty;

            var href = nextButton.Attributes["href"]?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(href)) return string.Empty;

            return href;

        }

        public override string GetChapterTitle(HtmlDocument document, List<string> headerTextToRemove)
        {
            var headerNode = document.DocumentNode.SelectSingleNode("//span[@class='chapter-title']");

            if (headerNode == null) return string.Empty;

           
            if (string.IsNullOrEmpty(headerNode.InnerText))
                return string.Empty;

            var decodedTitle = HttpUtility.HtmlDecode(headerNode.InnerText);

            foreach (var item in headerTextToRemove)
            {
                decodedTitle = decodedTitle.Replace(item, "");
            }

            return decodedTitle;
            
        }
    }
}
