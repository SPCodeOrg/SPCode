using System;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace SPCode.Interop.Updater
{
    public static class UpdateCheck
    {
        public static async Task Check()
        {
            if (Program.UpdateStatus != null)
            {
                if (Program.UpdateStatus.IsAvailable)
                {
                    return;
                }
            }

            await CheckInternal();
        }

        private static async Task CheckInternal()
        {
            var info = new UpdateInfo();
            try
            {
                var latestVer = await GetLatest();
                if (!IsUpToDate(Assembly.GetEntryAssembly()?.GetName().Version, Version.Parse(latestVer.TagName)))
                {
                    info.Release = latestVer;
                    if (info.Asset == null)
                    {
                        throw new Exception("Unable to find a valid asset!");
                    }
                    info.IsAvailable = true;
                }
                else
                {
                    info.IsAvailable = false;
                }
            }
            catch (Exception e)
            {
                info.IsAvailable = false;
                info.GotException = true;
                info.ExceptionMessage = e.Message;
            }

            lock (Program.UpdateStatus) //since multiple checks can occur, we'll wont override another ones...
            {
                if (Program.UpdateStatus.WriteAble)
                {
                    Program.UpdateStatus = info;
                }
            }
        }


        /*
         * 0 -> Major
         * 1 -> Minor
         * 2 -> Build
         * 3 -> Revision
         */
        private static bool IsUpToDate(Version currentVer, Version latestVer)
        {
            return currentVer.CompareTo(latestVer) >= 0;
        }

        private static async Task<Release> GetLatest()
        {
            var client = new GitHubClient(new ProductHeaderValue("spcode-client"));
            var releases = await client.Repository.Release.GetAll("Hexer10", "SPCode");
            return releases[0];
        }
    }
}