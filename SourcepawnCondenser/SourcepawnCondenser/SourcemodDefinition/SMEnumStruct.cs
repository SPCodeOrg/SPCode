using System.Collections.Generic;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct : SMClasslike
    {
    }

    public class SMEnumStructField : SMObjectField
    {
        public string MethodmapName = string.Empty;
        //public string Type = string.Empty; not needed yet
    }

    public class SMEnumStructMethod : SMObjectMethod
    {
    }
}