namespace Happy.Reader
{
    public class Chapter
    {
        public string NextChapter { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public List<string> Paragraphs { get; set; } = new List<string>();
        public byte[] TTS { get; set; } = [];
    }
}
