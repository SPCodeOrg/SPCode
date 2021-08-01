using System;
using System.Diagnostics;

namespace SPCode.Utils
{
    public class JavaUtils
    {
        private const int JavaVersionForLysis = 11;

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

            return int.Parse(output.Split(' ')[1].Split('.')[0]) < JavaVersionForLysis ? JavaResults.Outdated : JavaResults.Correct;
        }
    }
}
