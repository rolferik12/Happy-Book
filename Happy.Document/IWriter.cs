namespace Happy.Document
{
    using Happy.Reader;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IWriter
    {
        public void WriteChapter(Chapter chapter);
        public void Save();
    }
}
