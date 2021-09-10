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
        /// <summary>
        /// Calls the CheckInternal function to set the IsAvailable flag.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Compares versions by getting all releases to see if there's an update available or not.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Compares versions.
        /// </summary>
        /// <param name="currentVer"></param>
        /// <param name="latestVer"></param>
        /// <returns></returns>
        private static bool IsUpToDate(Version currentVer, Version latestVer)
        {
            return currentVer.CompareTo(latestVer) >= 0;
        }

        /// <summary>
        /// Calls the GitHub API to get all releases.
        /// </summary>
        /// <returns></returns>
        private static async Task<List<Release>> GetAllReleases()
        {
            var client = new GitHubClient(new ProductHeaderValue("spcode-client"));
            var allReleases = await client.Repository.Release.GetAll("SPCodeOrg", "SPCode");
            var list = new List<Release>();
            foreach (var item in allReleases)
            {
                list.Add(item);
            }
            return list.Take(10).ToList();
        }
    }
}