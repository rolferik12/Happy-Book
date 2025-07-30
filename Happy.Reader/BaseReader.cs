namespace Happy.Reader
{
    using HtmlAgilityPack;
    using KokoroSharp;
    using KokoroSharp.Core;
    using KokoroSharp.Utilities;
    using Microsoft.ML.OnnxRuntime;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.Metrics;
    using System.Net;
    using System.Reflection;
    using System.Text;

    public abstract class BaseReader
    {

        public abstract string Domain { get; }
        public string Url { get; set; }
        public string BookName { get; set; }

        private bool _tts = false;

        private List<string> headerTextToRemove = new List<string>();

        public BaseReader(string url, string bookName, string removeHeaderText = "", bool tts = false)
        {
            Url = url;
            BookName = bookName;
            _tts = tts;


            if (tts)
            {
                Task.Run(() =>
                {
                    KokoroTTS.LoadModel();
                });
            }

            headerTextToRemove = removeHeaderText.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public async IAsyncEnumerable<Chapter> GetChapters(int chapterCount, string ttsSavePath = "")
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

                    var title = GetChapterTitle(doc, headerTextToRemove);
                    var paragraphs = GetParagraphs(doc, title).ToList();


                    var chapter = new Chapter
                    {
                        NextChapter = GetNextChapterLink(doc),
                        Html = GetChapterHtml(doc),
                        Title = title,
                        Paragraphs = paragraphs,
                    };

                    if (_tts)
                    {
                        var savedTTs = await SaveTtsForChapter(chapter.Title, chapter.Paragraphs.ToList(), ttsSavePath);
                    }


                    nextUrl = chapter.NextChapter;
                    count++;

                    yield return chapter;
                }
            }
        }

        private async ValueTask<bool> SaveTtsForChapter(string title, List<string> paragraphs, string savePath)
        {
            if (paragraphs.Count == 0) return false;

            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (currentPath == null) return false;

            var options = new SessionOptions();
            options.AppendExecutionProvider_CUDA();


            KokoroVoice voice = KokoroVoiceManager.GetVoice("af_heart").MixWith(KokoroVoiceManager.GetVoice("af_nicole"), 0.8f, 0.2f);

            var text = string.Join(".\n", paragraphs).Replace("’", "'");
            StringBuilder sb = new StringBuilder();
            sb.Append($"{title}. ");
            sb.Append(text);

            var indexCount = sb.Length / 5000;

            using (var synth = new KokoroWavSynthesizer($@"{currentPath}\kokoro.onnx", options))
            {
                if (synth == null) return false;
                var tts = await synth.SynthesizeAsync(sb.ToString(), voice);

                if (tts == null) return false;

                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                var fileName = $@"{savePath}\{title.RemoveSpecialCharacters()}";
                var soundFilePath = $"{fileName}.wav";
                if (File.Exists(soundFilePath))
                    File.Delete(soundFilePath);

                try
                {
                    synth.SaveAudioToFile(tts, soundFilePath);
                }
                catch (Exception)
                {
                    if (!File.Exists(soundFilePath))
                        throw;
                }
                finally
                {
                    synth.Dispose();
                }


                File.WriteAllText($"{fileName}.txt", text);

                return true;
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

                if (child.Name != "p" && child.HasChildNodes)
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

        public abstract IEnumerable<string> GetParagraphs(HtmlDocument document, string title = "");
        public abstract string GetChapterHtml(HtmlDocument document);
        public abstract string GetNextChapterLink(HtmlDocument document);
        public abstract string GetChapterTitle(HtmlDocument document, List<string> headerTextToRemove);
    }
}
