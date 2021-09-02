using System;
using System.Collections.Generic;
using System.Linq;
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
                info.AllReleases = await GetAllReleases();
                if (!IsUpToDate(Assembly.GetEntryAssembly()?.GetName().Version, Version.Parse(info.AllReleases[0].TagName)))
                {
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

            lock (Program.UpdateStatus)
            {
                if (Program.UpdateStatus.WriteAble)
                {
                    Program.UpdateStatus = info;
                }
            }
        }

        private static bool IsUpToDate(Version currentVer, Version latestVer)
        {
            return currentVer.CompareTo(latestVer) >= 0;
        }

        private static async Task<List<Release>> GetAllReleases()
        {
            var client = new GitHubClient(new ProductHeaderValue("spcode-client"));
            var allReleases = await client.Repository.Release.GetAll("Hexer10", "SPCode");
            var list = new List<Release>();
            foreach (var item in allReleases)
            {
                list.Add(item);
            }
            return list.Take(10).ToList();
        }
    }
}