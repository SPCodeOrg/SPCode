using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
    public class ObjectBrowserTag
    {
        public ObjectBrowserItemKind Kind;
        public string? Value;
    }

    public enum ObjectBrowserItemKind
    {
        ParentDirectory,
        Directory,
        File,
        Empty
    }
}
