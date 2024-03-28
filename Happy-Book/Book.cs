using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Happy_Book
{
    internal class Book
    {
        public string Title { get; set; } = string.Empty;
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
    }
}
