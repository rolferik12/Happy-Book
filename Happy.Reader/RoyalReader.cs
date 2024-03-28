namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Web;

    public class RoyalReader : BaseReader
    {

        public override string Domain { get; } = "https://www.royalroad.com";

        public RoyalReader(string url, string bookName) : base(url, bookName)
        {
        }

        public override string GetChapterHtml(HtmlDocument document)
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']");

            if (chapterNode == null) return string.Empty;

            return chapterNode.InnerHtml;
        }

        public override string GetNextChapterLink(HtmlDocument document)
        {
            var navButtonsNode = document.DocumentNode.SelectSingleNode("//div[@class='row nav-buttons']");
            if (navButtonsNode == null) return string.Empty;
            if (!navButtonsNode.HasChildNodes) return string.Empty;

            var divs = navButtonsNode.ChildNodes.Where(node => node.Name == "div");

            if (divs.Count() == 0) return string.Empty;

            var nextButton = divs.LastOrDefault();

            if (nextButton == null) return string.Empty;

            var link = nextButton.ChildNodes.Where(node => node.Name == "a").FirstOrDefault();

            if (link == null) return string.Empty;

            var href = link.Attributes["href"].Value;

            if (string.IsNullOrEmpty(href)) return string.Empty;

            return href;

        }

        public override string GetChapterTitle(HtmlDocument document)
        {
            var headerNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'fic-header')]");
            var h1Node = headerNode.SelectSingleNode("//h1");
            if (!string.IsNullOrEmpty(h1Node.InnerText))
                return HttpUtility.HtmlDecode(h1Node.InnerText);

            return string.Empty;
        }
    }
}
