using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Happy_Book
{
    public record HappyText
    {
        public bool Bold { get; set; }
        public bool Cursive { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
