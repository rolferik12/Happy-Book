namespace Happy.Document
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IWriter
    {
        public void WriteChapterFromHtml(string title, string html);
        public void Save();
    }
}
