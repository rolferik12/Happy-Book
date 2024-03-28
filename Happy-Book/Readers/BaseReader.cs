using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Happy_Book.Readers
{
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
                    var chapter = GetChapter(doc);
                    nextUrl = $"{Domain}{chapter.NextChapter}";
                    count++;

                    yield return chapter;
                }
            }
        }

        public Chapter GetChapter(HtmlDocument document)
        {
            return new Chapter
            {
                NextChapter = GetNextChapterLink(document),
                html = GetChapterHtml(document),
                Title = GetChapterTitle(document)
            };
        }
        public abstract string GetChapterHtml(HtmlDocument document);
        public abstract string GetNextChapterLink(HtmlDocument document);
        public abstract HappyText GetChapterTitle(HtmlDocument document);
    }
}
