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
        public TableFormatMode TableFormatMode { get; set; } = TableFormatMode.NoFormatting;
        public bool TreatTwoColumnTablesAsSmall { get; set; } = false;

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
                    title = CapitalizeFirstLetter(title);
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
            return CapitalizeFirstLetter(title);
        }

        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Trim();
            if (text.Length == 0)
                return text;

            // Find the first letter character
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetter(text[i]))
                {
                    if (char.IsLower(text[i]))
                    {
                        return text.Substring(0, i) + char.ToUpper(text[i]) + text.Substring(i + 1);
                    }
                    // Already uppercase
                    return text;
                }
            }

            // No letters found
            return text;
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

        internal void FormatTables(HtmlNode node)
        {
            if (TableFormatMode == TableFormatMode.NoFormatting)
                return;

            var tables = node.SelectNodes(".//table");
            if (tables == null || tables.Count == 0)
                return;

            foreach (var table in tables.ToList())
            {
                var formattedText = FormatTable(table);
                if (!string.IsNullOrEmpty(formattedText))
                {
                    var replacement = HtmlNode.CreateNode(formattedText);

                    // Get the parent and the position of the table
                    var parent = table.ParentNode;
                    var tableIndex = parent.ChildNodes.IndexOf(table);

                    // Replace the table
                    parent.ReplaceChild(replacement, table);

                    // Clean up whitespace/br tags immediately after the replacement
                    var nextIndex = tableIndex;
                    while (nextIndex < parent.ChildNodes.Count)
                    {
                        var nextNode = parent.ChildNodes[nextIndex];

                        // If it's a text node with only whitespace, remove it
                        if (nextNode.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(nextNode.InnerText))
                        {
                            nextNode.Remove();
                            continue;
                        }

                        // If it's a br tag, remove it
                        if (nextNode.Name == "br")
                        {
                            nextNode.Remove();
                            continue;
                        }

                        // Stop at the first non-whitespace, non-br element
                        break;
                    }
                }
            }
        }

        private string FormatTable(HtmlNode table)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows == null || rows.Count == 0)
                return string.Empty;

            var tableData = new List<List<HtmlNode>>();

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//th|.//td");
                if (cells == null || cells.Count == 0)
                    continue;

                var rowData = new List<HtmlNode>();
                foreach (var cell in cells)
                {
                    rowData.Add(cell);
                }
                tableData.Add(rowData);
            }

            if (tableData.Count == 0)
                return string.Empty;

            int rowCount = tableData.Count;
            int colCount = tableData.Max(r => r.Count);

            bool isSmallTable = (rowCount == 1 && colCount == 2) || 
                               (rowCount == 2 && colCount == 1) ||
                               (rowCount == 2 && colCount == 2) ||
                               (TreatTwoColumnTablesAsSmall && colCount == 2);

            if (isSmallTable)
            {
                return FormatSmallTable(tableData, rowCount, colCount);
            }
            else
            {
                return FormatLargeTable(tableData, rowCount, colCount);
            }
        }

        private string GetCellHtml(HtmlNode cell)
        {
            var html = cell.InnerHtml.Trim();

            // Replace closing </p> tags followed by opening <p> tags with <br/>
            html = System.Text.RegularExpressions.Regex.Replace(html, @"</p>\s*<p[^>]*>", "<br/>");

            // Remove any remaining opening <p> tags
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<p[^>]*>", "");

            // Remove any remaining closing </p> tags
            html = html.Replace("</p>", "");

            // Normalize br tags to <br/>
            html = html.Replace("<br>", "<br/>").Replace("<br />", "<br/>");

            // Clean up multiple consecutive <br/> tags (more than 2)
            html = System.Text.RegularExpressions.Regex.Replace(html, @"(<br\s*/?>){3,}", "<br/><br/>");

            // Remove ALL leading and trailing <br/> tags (not just one)
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^(<br\s*/?>)+", "");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"(<br\s*/?>)+$", "");

            html = System.Web.HttpUtility.HtmlDecode(html);
            return html.Trim();
        }

        private string CleanCellText(string html, bool isHeader)
        {
            // For headers, we want to strip out strong/b tags since we'll re-apply them
            // This prevents nested <strong> tags
            if (isHeader)
            {
                // Extract text from <strong> and <b> tags but keep other formatting like <em>, <i>
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<strong[^>]*>(.*?)</strong>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<b[^>]*>(.*?)</b>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // Clean up multiple consecutive spaces (but keep <br/> tags intact)
            html = System.Text.RegularExpressions.Regex.Replace(html, @"[ \t]{2,}", " ");

            // Clean up spaces around <br/> tags
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*<br\s*/?\>\s*", "<br/>");

            html = html.Trim();

            return html;
        }

        private string FormatSmallTable(List<List<HtmlNode>> tableData, int rowCount, int colCount)
        {
            var result = new System.Text.StringBuilder();
            result.Append("<p>");

            if (rowCount == 1 && colCount == 2)
            {
                var headerHtml = CleanCellText(GetCellHtml(tableData[0][0]), true);
                var valueHtml = CleanCellText(GetCellHtml(tableData[0][1]), false);
                result.Append($"<strong>{headerHtml}</strong><br/>{valueHtml}");
            }
            else if (rowCount == 2 && colCount == 1)
            {
                var headerHtml = CleanCellText(GetCellHtml(tableData[0][0]), true);
                var valueHtml = CleanCellText(GetCellHtml(tableData[1][0]), false);
                result.Append($"<strong>{headerHtml}</strong><br/>{valueHtml}");
            }
            else if (colCount == 2)
            {
                bool first = true;
                for (int row = 0; row < rowCount; row++)
                {
                    if (tableData[row].Count >= 2)
                    {
                        var headerHtml = CleanCellText(GetCellHtml(tableData[row][0]), true);
                        var valueHtml = CleanCellText(GetCellHtml(tableData[row][1]), false);
                        if (!string.IsNullOrWhiteSpace(tableData[row][0].InnerText) || !string.IsNullOrWhiteSpace(tableData[row][1].InnerText))
                        {
                            if (!first)
                                result.Append("<br/><br/>");
                            result.Append($"<strong>{headerHtml}</strong><br/>{valueHtml}");
                            first = false;
                        }
                    }
                }
            }

            result.Append("</p>");
            return result.ToString();
        }

        private string FormatLargeTable(List<List<HtmlNode>> tableData, int rowCount, int colCount)
        {
            var result = new System.Text.StringBuilder();
            result.Append("<p>");

            if (TableFormatMode == TableFormatMode.ColumnFirst)
            {
                for (int col = 0; col < colCount; col++)
                {
                    if (col > 0)
                        result.Append("<br/>---<br/>");

                    for (int row = 0; row < rowCount; row++)
                    {
                        if (col < tableData[row].Count)
                        {
                            var cellHtml = CleanCellText(GetCellHtml(tableData[row][col]), false);
                            if (!string.IsNullOrWhiteSpace(tableData[row][col].InnerText))
                            {
                                if (row > 0)
                                    result.Append("<br/>");

                                if (row == 0)
                                {
                                    // Strip any strong tags from the cell content for row 0 before re-applying
                                    var headerText = System.Text.RegularExpressions.Regex.Replace(cellHtml, @"<strong[^>]*>(.*?)</strong>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    headerText = System.Text.RegularExpressions.Regex.Replace(headerText, @"<b[^>]*>(.*?)</b>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    result.Append($"<strong>{headerText}</strong>");
                                }
                                else
                                    result.Append(cellHtml);
                            }
                        }
                    }
                }
            }
            else if (TableFormatMode == TableFormatMode.RowFirst)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    if (row > 0)
                        result.Append("<br/>---<br/>");

                    for (int col = 0; col < tableData[row].Count; col++)
                    {
                        var cellHtml = CleanCellText(GetCellHtml(tableData[row][col]), false);
                        if (!string.IsNullOrWhiteSpace(tableData[row][col].InnerText))
                        {
                            if (col > 0)
                                result.Append("<br/>");

                            if (row == 0)
                            {
                                // Strip any strong tags from the cell content for row 0 before re-applying
                                var headerText = System.Text.RegularExpressions.Regex.Replace(cellHtml, @"<strong[^>]*>(.*?)</strong>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                headerText = System.Text.RegularExpressions.Regex.Replace(headerText, @"<b[^>]*>(.*?)</b>", "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                result.Append($"<strong>{headerText}</strong>");
                            }
                            else
                                result.Append(cellHtml);
                        }
                    }
                }
            }

            result.Append("</p>");
            return result.ToString();
        }

        public abstract IEnumerable<string> GetParagraphs(HtmlDocument document, string title = "");
        public abstract string GetChapterHtml(HtmlDocument document);
        public abstract string GetNextChapterLink(HtmlDocument document);
        public abstract string GetChapterTitle(HtmlDocument document, List<string> headerTextToRemove);
    }
}
