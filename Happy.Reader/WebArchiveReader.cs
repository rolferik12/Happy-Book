namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Web;

    public class WebArchiveRoyalReader : BaseReader
    {

        public override string Domain { get; } = "https://web.archive.org";

        public WebArchiveRoyalReader(string url, string bookName, string headerToRemove) : base(url, bookName, headerToRemove)
        {
        }

        public override string GetChapterHtml(HtmlDocument document)
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']");

            if (chapterNode == null) return string.Empty;

            var swearWords = new Dictionary<string, int>
            {
                { "Amazon", 9 },
                { "Royal Road", 9 },
                { "stolen", 6 },
                { "novel", 3},
                { "report", 3 },
                { "pilfered", 3 },
                { "violation", 3 },
                { "content", 3 },
                { "unauthorized", 4 },
                { "theft", 3 },
                { "detected", 3 },
                { "story", 3 },
                { "without authorization", 5 },
                { "consent", 3 },
                { "narrative", 3 }
            };

            RemoveNodeWithTextProbability(chapterNode, swearWords);

            ChangeTableWidth(chapterNode, 100);

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

        public override string GetChapterTitle(HtmlDocument document, List<string> headerTextToRemove)
        {
            var headerNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'fic-header')]");

            if (headerNode == null) return string.Empty;

            var h1Node = headerNode.SelectSingleNode("//h1");
            if (string.IsNullOrEmpty(h1Node.InnerText))
                return string.Empty;

            var decodedTitle = HttpUtility.HtmlDecode(h1Node.InnerText);

            foreach (var item in headerTextToRemove)
            {
                decodedTitle = decodedTitle.Replace(item, "");
            }

            return decodedTitle;

        }

        public override IEnumerable<string> GetParagraphs(HtmlDocument document, string title = "")
        {
            return new List<string>();
        }
    }
}
