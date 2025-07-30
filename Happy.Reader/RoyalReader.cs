namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Web;

    public class RoyalReader : BaseReader
    {

        public override string Domain { get; } = "https://www.royalroad.com";

        public RoyalReader(string url, string bookName, string headerToRemove, bool tts = false) : base(url, bookName, headerToRemove, tts)
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
                { "unlawful", 5 },
                { "author", 3 },
                { "consent", 3 },
                { "narrative", 3 },
                { "published elsewhere", 5 },
                { "Support the author", 5 }
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
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']");

            if (chapterNode == null) return [];

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
                { "unlawful", 5 },
                { "author", 3 },
                { "consent", 3 },
                { "narrative", 3 },
                { "published elsewhere", 5 },
                { "Support the author", 5 }
            };

            RemoveNodeWithTextProbability(chapterNode, swearWords);

            var paragraphNodes = chapterNode.ChildNodes.Where((child) => child.Name == "p");
            var paragraphs = new List<string>();

            if (paragraphNodes == null) return [];

            foreach (var paragraphNode in paragraphNodes)
            {
                if (paragraphNode.InnerHtml.Contains("<br"))
                    paragraphNode.InnerHtml = paragraphNode.InnerHtml.Replace("<br>", "\n").Replace("<br/>", "\n");

                var inner = paragraphNode.InnerText.Split("\n", StringSplitOptions.RemoveEmptyEntries);

                paragraphs.AddRange(inner);
            }




            paragraphs = CleanParagraphs(paragraphs, title);

            return paragraphs;
        }

        private List<string> CleanParagraphs(List<string> paragraphs, string title)
        {
            var cleanedList = new List<string>();

            foreach (var paragraph in paragraphs)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    if (paragraph.ToLower().Equals(title.ToLower()))
                        continue;
                }

                var cleanedParagraph = paragraph.Replace("\n", "").Replace("\r", "");

                cleanedParagraph = ReplaceNumbers(cleanedParagraph);
                cleanedParagraph = ReplaceFractions(cleanedParagraph);
                cleanedParagraph = ReplaceFractionPer(cleanedParagraph);
                cleanedList.Add(cleanedParagraph);
            }

            return cleanedList;
        }

        private string ReplaceNumbers(string cleanedParagraph)
        {
            var replaced = cleanedParagraph;
            var regex = new Regex("(\\d+,\\d+)");

            while (regex.IsMatch(replaced))
            {
                var match = regex.Match(replaced);
                var substring = replaced.Substring(match.Index, match.Value.Length);

                if (!substring.Contains(",")) continue;

                var cleanedNumber = match.Value.Replace(",", "");
                replaced = replaced.Replace(match.Value, cleanedNumber);
            }

            return replaced;
        }

        private string ReplaceFractions(string cleanedParagraph)
        {
            var replaced = cleanedParagraph;
            var regex = new Regex("(\\d+\\/\\d+)");

            while (regex.IsMatch(replaced))
            {
                var match = regex.Match(replaced);
                var substring = replaced.Substring(match.Index, match.Value.Length);

                if (!substring.Contains("/")) continue;

                var cleaned = match.Value.Replace("/", " of ");
                replaced = replaced.Replace(match.Value, cleaned);
            }

            return replaced;
        }

        private string ReplaceFractionPer(string cleanedParagraph)
        {
            var replaced = cleanedParagraph;
            var regex = new Regex("(\\d+\\/)(hour|second|minute)");

            while (regex.IsMatch(replaced))
            {
                var match = regex.Match(replaced);
                var substring = replaced.Substring(match.Index, match.Value.Length);

                if (!substring.Contains("/")) continue;

                var cleaned = match.Value.Replace("/", " per ");
                replaced = replaced.Replace(match.Value, cleaned);
            }

            return replaced;
        }
    }
}
