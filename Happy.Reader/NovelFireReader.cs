namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Web;

    public class NovelFireReader : BaseReader
    {

        public override string Domain { get; } = "";

        public NovelFireReader(string url, string bookName, string headerToRemove, bool tts = false) : base(url, bookName, headerToRemove, tts: tts)
        {
        }

        public override string GetChapterHtml(HtmlDocument document)
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@id='content']");

            if (chapterNode == null) return string.Empty;

            ChangeTableWidth(chapterNode, 100);

            var swearWords = new Dictionary<string, int>
            {
                { "website on google to access chapters of novels", 11 },
                { "for more, visit", 11 },
                { "search the", 5 },
                { "novelfire.net", 11 }
            };

            RemoveNodeWithTextProbability(chapterNode, swearWords);

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

        public override IEnumerable<string> GetParagraphs(HtmlDocument document, string title = "")
        {
            var chapterNode = document.DocumentNode.SelectSingleNode("//div[@id='content']");

            if (chapterNode == null) return new List<string>();

            var swearWords = new Dictionary<string, int>
            {
                { "website on google to access chapters of novels", 11 },
                { "for more, visit", 11 },
                { "search the", 5 },
                { "novelfire.net", 11 }
            };

            RemoveNodeWithTextProbability(chapterNode, swearWords);

            var paragraphNodes = chapterNode.ChildNodes.Where((child) => child.Name == "p");

            if (paragraphNodes == null) return new List<string>();

            var paragraphs = paragraphNodes.Select((p) => p.InnerText).ToList();

            paragraphs = CleanParagraphs(paragraphs);

            return paragraphs;
        }

        private List<string> CleanParagraphs(List<string> paragraphs)
        {
            var cleanedList = new List<string>();

            foreach (var paragraph in paragraphs)
            {
                var cleanedParagraph = paragraph.Replace("\n", "").Replace("\r", "");

                cleanedParagraph = ReplaceNumbers(cleanedParagraph);
                cleanedList.Add(cleanedParagraph);
            }

            return cleanedList;
        }

        private string ReplaceNumbers(string cleanedParagraph)
        {
            var replaced = cleanedParagraph;
            var regex = new Regex("\\d*,\\d");

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
    }
}
