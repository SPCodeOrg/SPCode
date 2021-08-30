using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
    public class ErrorDataGridRow
    {
        public string File { set; get; }
        public string Line { set; get; }
        public string Type { set; get; }
        public string Details { set; get; }
    }
}
