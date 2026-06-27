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
        public List<string> FailedChapters { get; }
        public Task WriteChapterAsync(Chapter chapter);
        public void Save();
    }
}
