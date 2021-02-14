using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SPCode.Utils
{
    public class JavaUtils
    {
        private static readonly int JavaVersionForLysis = 11;

        public enum JavaResults
        {
            Outdated,
            Absent,
            Correct
        }

        public JavaResults GetJavaStatus()
        {
            var process = new Process();
            process.StartInfo.FileName = "javac";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = "-version";

            string output;
            try
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception) // no java in the PATH directory
            {
                return JavaResults.Absent;
            }

            if (int.Parse(output.Split(' ')[1].Split('.')[0]) < JavaVersionForLysis)
            {
                return JavaResults.Outdated;
            }
            else
            {
                return JavaResults.Correct;
            }
        }
    }
}
