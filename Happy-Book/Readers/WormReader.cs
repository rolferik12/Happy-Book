using HtmlAgilityPack;
using System.Web;

namespace Happy_Book.Readers
{
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

        private void RemoveNodeWithtext(HtmlNode node, params string[] phrases)
        {
            var children = node.ChildNodes;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];

                foreach (var phrase in phrases)
                {
                    if (!child.InnerText.Contains(phrase)) continue;

                    node.RemoveChild(child);
                    i--;
                    break;

                }
            }
        }

        private void RemoveLink(HtmlNode node)
        {
            var children = node.ChildNodes;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Attributes["href"] == null)
                {
                    RemoveLink(child);
                    continue;
                }

                node.RemoveChild(child);
                i--;
            }
        }

        public override HappyText GetChapterTitle(HtmlDocument document)
        {
            var h1Node = document.DocumentNode.SelectSingleNode("//h1[@class='entry-title']");

            if (!string.IsNullOrEmpty(h1Node.InnerText))
                return new HappyText { Value = HttpUtility.HtmlDecode(h1Node.InnerText) };

            return new HappyText();
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
