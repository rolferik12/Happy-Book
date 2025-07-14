namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Diagnostics.Metrics;
    using System.Net;

    public abstract class BaseReader
    {
        public abstract string Domain { get; }
        public string Url { get; set; }
        public string BookName { get; set; }

        private List<string> headerTextToRemove = new List<string>();

        public BaseReader(string url, string bookName, string removeHeaderText = "")
        {
            Url = url;
            BookName = bookName;

            headerTextToRemove = removeHeaderText.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public async IAsyncEnumerable<Chapter> GetChapters(int chapterCount)
        {
            var nextUrl = Url;
            int count = 0;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("user-agent",
                    "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                while (count < chapterCount && !string.IsNullOrEmpty(nextUrl))
                {
                    var html = string.Empty;
                    try
                    {
                        var response = await client.GetAsync($"{Domain}{nextUrl}");

                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            await Task.Delay(5000);
                            continue;
                        }

                        html = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }


                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var chapter = new Chapter
                    {
                        NextChapter = GetNextChapterLink(doc),
                        Html = GetChapterHtml(doc),
                        Title = GetChapterTitle(doc, headerTextToRemove)
                    };

                    nextUrl = chapter.NextChapter;
                    count++;

                    yield return chapter;
                }
            }
        }

        public async Task<Chapter> GetChapterAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("user-agent",
                    "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                var html = string.Empty;
                try
                {
                    var response = await client.GetAsync($"{Domain}{Url}");

                    html = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }


                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var chapter = new Chapter
                {
                    NextChapter = GetNextChapterLink(doc),
                    Html = GetChapterHtml(doc),
                    Title = GetChapterTitle(doc, headerTextToRemove)
                };

                return chapter;
            }
        }

        internal void ChangeTableWidth(HtmlNode node, int percentage)
        {
            var children = node.ChildNodes;

            foreach (var child in children)
            {
                if (child.Name != "table")
                {
                    ChangeTableWidth(child, percentage);
                    continue;
                }

                child.Attributes.Add("style", "width: 100%");
            }
        }

        internal void RemoveNodeWithTextProbability(HtmlNode node, Dictionary<string, int> keywords, int scoreMax = 9)
        {
            var children = node.ChildNodes;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];

                if (child.HasChildNodes)
                {
                    RemoveNodeWithTextProbability(child, keywords);
                    continue;
                }

                int score = 0;
                foreach (var keyword in keywords)
                {
                    if (child.InnerText.ToLower().Contains(keyword.Key.ToLower()))
                        score += keyword.Value;
                }

                if (score > 3 && score <= scoreMax)
                {
                    continue;
                }

                if (score > scoreMax)
                {
                    node.RemoveChild(child);
                    continue;
                }
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
        public abstract string GetChapterTitle(HtmlDocument document, List<string> headerTextToRemove);
    }
}
