namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Web;

    public class WormReader : BaseReader
    {
        public WormReader(string url, string bookName) : base(url, bookName)
        {
        }

        public override string Domain => string.Empty;

        public override string GetChapterHtml(HtmlDocument document)
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@class='entry-content']");

            if (chapterNode == null) return string.Empty;

            RemoveLink(chapterNode);
            RemoveNodeWithtext(chapterNode, "Share this:", "Like Loading...");

            return chapterNode.InnerHtml;
        }

        public override string GetChapterTitle(HtmlDocument document)
        {
            var h1Node = document.DocumentNode.SelectSingleNode("//h1[@class='entry-title']");

            if (!string.IsNullOrEmpty(h1Node.InnerText))
                return HttpUtility.HtmlDecode(h1Node.InnerText);

            return string.Empty;
        }

        public override string GetNextChapterLink(HtmlDocument document)
        {
            var links = document.DocumentNode.SelectNodes("//a");

            foreach (var link in links)
            {
                if (link.InnerText != "Next Chapter") continue;

                var href = link.Attributes["href"].Value;

                if (string.IsNullOrEmpty(href)) return string.Empty;

                return href;
            }

            return string.Empty;
        }
    }
}
