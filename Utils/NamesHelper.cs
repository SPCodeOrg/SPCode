using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
    public static class NamesHelper
    {
#if BETA
        public static bool Beta = false;
#else
        public static bool Beta = true; 
#endif
        public static string ProgramPublicName => Beta ? "SPCode Beta" : "SPCode";
        public static string PipeServerName => Beta ? "SPCodeBetaNamedPipeServer" : "SPCodeNamedPipeServer";
    }
}
