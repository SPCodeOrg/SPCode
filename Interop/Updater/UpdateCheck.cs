using SPCode.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
                info.AllReleases = (await GetAllReleases()).ToList();

#if BETA
                var currentVersion = VersionHelper.GetRevisionNumber();
                var latestVersion = VersionHelper.GetRevisionNumber(info.AllReleases[0].TagName);
#else
                var currentVersion = VersionHelper.GetAssemblyVersion();
                var latestVersion = new Version(info.AllReleases[0].TagName);
#endif

                if (IsUpToDate(currentVersion, latestVersion))
                {
                    info.IsAvailable = false;
                }
                else
                {
                    if (info.Asset == null)
                    {
                        throw new Exception("A new release was pushed, but no valid update assets were found.");
                    }
                    info.IsAvailable = true;
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
        private static bool IsUpToDate(object currentVer, object latestVer)
        {
            // If we're comparing beta versions, compare revision numbers
#if BETA
            return Convert.ToInt32(currentVer) >= Convert.ToInt32(latestVer);
#else
            return ((Version)currentVer).CompareTo((Version)latestVer) >= 0;
#endif
        }

        /// <summary>
        /// Calls the GitHub API to get all releases.
        /// </summary>
        /// <returns></returns>
        private static async Task<IEnumerable<Release>> GetAllReleases()
        {
            var apiOptions = new ApiOptions()
            {
                PageCount = 1,
                PageSize = 10
            };

            var client = new GitHubClient(new ProductHeaderValue("spcode-client"));
            var releases = await client.Repository.Release.GetAll("SPCodeOrg", "SPCode", apiOptions);
#if BETA
            var finalReleasesList = releases.Where(x => x.Prerelease);
#else
            var finalReleasesList = releases.Where(x => !x.Prerelease);
#endif
            return finalReleasesList;
        }
    }
}