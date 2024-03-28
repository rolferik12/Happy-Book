using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Happy_Book
{
    public class Chapter
    {
        public string NextChapter { get; set; } = string.Empty;
        public HappyText Title { get; set; } = new HappyText();
        public string Name { get; set; } = string.Empty;
        public List<HappyParagraph> Paragraphs { get; set; } = new List<HappyParagraph>();

        public string html { get; set; }
    }
}
