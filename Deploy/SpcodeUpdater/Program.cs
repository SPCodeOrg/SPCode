using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace SPCodeUpdater
{
    public static class Program
    {
        public delegate void InvokeDel();

        [STAThread]
        public static void Main()
        {
            var processes = Process.GetProcesses().Where(x => x.ProcessName.Contains("SPCode"));
            foreach (var process in processes)
            {
                try
                {
                    process.WaitForExit();
                }
                catch (Exception)
                {

                }
            }

            Application.EnableVisualStyles();
            Thread.Sleep(2000);
            Application.SetCompatibleTextRenderingDefault(true);
            var um = new UpdateMarquee();
            um.Show();
            Application.DoEvents(); //execute Visual
            var t = new Thread(Worker);
            t.Start(um);
            Application.Run(um);
        }

        private static void Worker(object arg)
        {
            try
            {
                var um = (UpdateMarquee)arg;

#if BETA
                var zipFile = ".\\SPCode.Beta.Portable.zip";
#else
                var zipFile = ".\\SPCode.Portable.zip";
#endif

                using (var fsInput = File.OpenRead(zipFile))
                using (var zf = new ZipFile(fsInput))
                {

                    foreach (ZipEntry zipEntry in zf)
                    {
                        if (zipEntry.IsDirectory || zipEntry.Name.Contains("sourcepawn"))
                        {
                            continue;
                        }

                        var entryFileName = zipEntry.Name;

                        var fullZipToPath = Path.Combine(@".\", entryFileName);
                        var directoryName = Path.GetDirectoryName(fullZipToPath);

                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        var buffer = new byte[4096];

                        using (var zipStream = zf.GetInputStream(zipEntry))
                        using (Stream fsOutput = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, fsOutput, buffer);
                        }
                    }
                }

                um.Invoke((InvokeDel)(() => { um.SetToReadyState(); }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The updater failed to update SPCode properly: {ex.Message}");
                Application.Exit();
            }
        }
    }
}