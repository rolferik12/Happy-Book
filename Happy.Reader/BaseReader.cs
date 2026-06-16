namespace Happy.Reader
{
    using HtmlAgilityPack;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public abstract class BaseReader
    {

        public abstract string Domain { get; }
        public string Url { get; set; }
        public string BookName { get; set; }

        private List<string> headerTextToRemove = new List<string>();
        private Dictionary<string, string> headerTextToReplace = new Dictionary<string, string>();
        private HashSet<string> hiddenCssClasses = new HashSet<string>();

        public BaseReader(string url, string bookName, string removeHeaderText = "", string replaceHeaderText = "")
        {
            Url = url;
            BookName = bookName;

            headerTextToRemove = removeHeaderText.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var entry in replaceHeaderText.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split('=', 2);
                if (parts.Length == 2)
                    headerTextToReplace[parts[0]] = parts[1];
            }
        }

        public async IAsyncEnumerable<Chapter> GetChapters(int chapterCount, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var nextUrl = Url;
            int count = 0;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("user-agent",
                    "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                while (count < chapterCount && !string.IsNullOrEmpty(nextUrl))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var html = string.Empty;
                    try
                    {
                        var requestUrl = ResolveUrl(nextUrl);
                        var response = await client.GetAsync(requestUrl, cancellationToken);

                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            await Task.Delay(5000, cancellationToken);
                            continue;
                        }

                        html = await response.Content.ReadAsStringAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }


                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var title = GetChapterTitle(doc, headerTextToRemove);
                    foreach (var kvp in headerTextToReplace)
                        title = title.Replace(kvp.Key, kvp.Value);
                    var paragraphs = GetParagraphs(doc, title).ToList();


                    var chapter = new Chapter
                    {
                        NextChapter = GetNextChapterLink(doc),
                        Html = GetChapterHtml(doc),
                        Title = title,
                        Paragraphs = paragraphs,
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
                    Title = ApplyHeaderReplacements(GetChapterTitle(doc, headerTextToRemove))
                };

                return chapter;
            }
        }

        private string ApplyHeaderReplacements(string title)
        {
            foreach (var kvp in headerTextToReplace)
                title = title.Replace(kvp.Key, kvp.Value);
            return title;
        }

        private string ResolveUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri)
                && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return url;
            }

            if (!string.IsNullOrEmpty(Domain))
            {
                var baseUri = new Uri(Domain);
                if (Uri.TryCreate(baseUri, url, out var resolved))
                    return resolved.AbsoluteUri;
            }

            return url;
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
            var children = node.ChildNodes.ToList();

            foreach (var child in children)
            {
                // Recursively check child nodes first
                if (child.HasChildNodes)
                {
                    RemoveNodeWithTextProbability(child, keywords, scoreMax);
                }

                // Skip if not a content element
                if (child.Name != "p" && child.Name != "div" && child.Name != "span")
                    continue;

                var textToCheck = GetNodeTextIncludingHidden(child);
                var isHidden = IsNodeHidden(child);

                int score = 0;
                foreach (var keyword in keywords)
                {
                    if (textToCheck.ToLower().Contains(keyword.Key.ToLower()))
                        score += keyword.Value;
                }

                // Remove if score exceeds threshold, OR if it's hidden and has ANY suspicious keywords
                if (score > scoreMax || (isHidden && score > 0))
                {
                    child.Remove();
                }
            }
        }

        internal string GetNodeTextIncludingHidden(HtmlNode node)
        {
            var allText = new System.Text.StringBuilder();

            void CollectText(HtmlNode current)
            {
                if (current.NodeType == HtmlNodeType.Text)
                {
                    allText.Append(current.InnerText);
                    return;
                }

                if (current.NodeType == HtmlNodeType.Element)
                {
                    foreach (var child in current.ChildNodes)
                    {
                        CollectText(child);
                    }
                }
            }

            CollectText(node);
            return allText.ToString();
        }

        internal bool IsNodeHidden(HtmlNode node)
        {
            if (node.NodeType != HtmlNodeType.Element)
                return false;

            // Check for 'hidden' attribute
            if (node.Attributes.Contains("hidden"))
                return true;

            // Check inline style for display:none or visibility:hidden
            var styleAttr = node.GetAttributeValue("style", "");
            if (!string.IsNullOrEmpty(styleAttr))
            {
                var styleLower = styleAttr.ToLower().Replace(" ", "");
                if (styleLower.Contains("display:none") || 
                    styleLower.Contains("visibility:hidden") ||
                    styleLower.Contains("opacity:0"))
                    return true;
            }

            // Check CSS classes that commonly indicate hidden content
            var classAttr = node.GetAttributeValue("class", "");
            if (!string.IsNullOrEmpty(classAttr))
            {
                var classLower = classAttr.ToLower();

                // Check common hidden class names
                var hiddenClasses = new[] { "hidden", "d-none", "hide", "invisible", "sr-only", "visually-hidden" };
                foreach (var hiddenClass in hiddenClasses)
                {
                    if (classLower.Contains(hiddenClass))
                        return true;
                }

                // Check against CSS classes parsed from style tags (for obfuscated/random class names)
                var classes = classAttr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cls in classes)
                {
                    if (hiddenCssClasses.Contains(cls))
                        return true;
                }
            }

            return false;
        }

        internal void ExtractHiddenCssClasses(HtmlDocument document)
        {
            hiddenCssClasses.Clear();

            // Find all <style> tags in the document
            var styleNodes = document.DocumentNode.SelectNodes("//style");
            if (styleNodes == null)
                return;

            foreach (var styleNode in styleNodes)
            {
                var cssContent = styleNode.InnerText;
                if (string.IsNullOrWhiteSpace(cssContent))
                    continue;

                // Parse CSS to find classes with hiding rules
                ParseCssForHiddenClasses(cssContent);
            }
        }

        private void ParseCssForHiddenClasses(string css)
        {
            // Use regex to find CSS rules: selector { rules }
            // This handles multi-line CSS and complex selectors better
            var rulePattern = @"\.([^\s\{\.#\[\]:>+~]+)\s*\{([^\}]+)\}";
            var matches = System.Text.RegularExpressions.Regex.Matches(css, rulePattern, System.Text.RegularExpressions.RegexOptions.Singleline);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count < 3)
                    continue;

                var className = match.Groups[1].Value;
                var rules = match.Groups[2].Value.ToLower();

                // Check if this rule hides content
                if (rules.Contains("display") && rules.Contains("none") ||
                    rules.Contains("visibility") && rules.Contains("hidden") ||
                    rules.Contains("opacity") && (rules.Contains(":0") || rules.Contains(": 0")))
                {
                    hiddenCssClasses.Add(className);
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
