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
            var nextUrl = $"{Domain}{Url}";
            int count = 0;
            using (var client = new HttpClient())
            {
                while (!string.IsNullOrEmpty(nextUrl) && count < chapterCount)
                {
                    var html = await client.GetStringAsync(nextUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var chapter = new Chapter
                    {
                        NextChapter = GetNextChapterLink(doc),
                        Html = GetChapterHtml(doc),
                        Title = GetChapterTitle(doc)
                    };

                    nextUrl = $"{Domain}{chapter.NextChapter}";
                    count++;

                    yield return chapter;
                }
            }
        }

        public abstract string GetChapterHtml(HtmlDocument document);
        public abstract string GetNextChapterLink(HtmlDocument document);
        public abstract string GetChapterTitle(HtmlDocument document);
    }
}
