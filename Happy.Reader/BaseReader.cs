namespace Happy.Reader
{
    using HtmlAgilityPack;

    public abstract class BaseReader
    {
        public abstract string Domain { get; }
        public string Url { get; set; }
        public string BookName { get; set; }

        public BaseReader(string url, string bookName)
        {
            Url = url;
            BookName = bookName;
        }

        public async IAsyncEnumerable<Chapter> GetChapters(int chapterCount)
        {
            var nextUrl = Url;
            int count = 0;
            using (var client = new HttpClient())
            {
                while (count < chapterCount && !string.IsNullOrEmpty(nextUrl))
                {
                    var html = await client.GetStringAsync($"{Domain}{nextUrl}");
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var chapter = new Chapter
                    {
                        NextChapter = GetNextChapterLink(doc),
                        Html = GetChapterHtml(doc),
                        Title = GetChapterTitle(doc)
                    };

                    nextUrl = chapter.NextChapter;
                    count++;

                    yield return chapter;
                }
            }
        }


        internal void RemoveNodeWithTextProbability(HtmlNode node, params string[] keywords)
        {
            var children = node.ChildNodes;
            
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                int score = 0;
                int weight = keywords.Length;
                foreach (var keyword in keywords)
                {
                    if (child.InnerText.ToLower().Contains(keyword.ToLower()))
                        score += weight;

                    weight--;
                }

                if (score > keywords.Length)
                {
                    node.RemoveChild(child);
                    continue;
                }

                RemoveNodeWithTextProbability(child, keywords);
            }
        }

        internal void RemoveNodeWithtext(HtmlNode node, params string[] phrases)
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

        internal void RemoveLink(HtmlNode node)
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

        public abstract string GetChapterHtml(HtmlDocument document);
        public abstract string GetNextChapterLink(HtmlDocument document);
        public abstract string GetChapterTitle(HtmlDocument document);
    }
}
