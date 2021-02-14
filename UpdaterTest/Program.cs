using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace UpdaterTest
{
    internal static class Program
    {
        private const string CurrentVersion = "1.3.5.0";

        public static async Task Main()
        {
            var lat = await GetLatest();
            var release = lat.Assets.First(e => e.Name == "SPCode.Portable.zip");
            Console.WriteLine(release.Id);
            Console.WriteLine(release.Label);
            Console.WriteLine(release.Name);
            Console.WriteLine(release.ContentType);
        }

        private static async Task Check()
        {
            try
            {
                var latestVer = await GetLatest();
                var release = latestVer.Assets.First(e => e.Name == "SPCode.Portable.zip");
                Console.WriteLine(release.BrowserDownloadUrl);

                using (var client = new WebClient())
                {
                    client.DownloadFile(release.BrowserDownloadUrl, "Update.zip");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Couldnt look for an update");
                return;
            }

            Directory.CreateDirectory("Update");
            var zip = ZipFile.OpenRead("Update.zip");

            // Dont override the sourcemod files
            var files = zip.Entries.Where(e => !e.FullName.StartsWith(@"sourcepawn\"));
            foreach (var file in files)
            {
                file.ExtractToFile("Update/" + file.FullName, true);
            }
        }
        /*
         * 0 -> Major
         * 1 -> Minor
         * 2 -> Build
         * 3 -> Revision
         */
        private static bool IsUpToDate(string current, string latest)
        {
            var currentSplit = current.Split('.').Select(int.Parse).ToList();
            var latestSplit = latest.Split('.').Select(int.Parse).ToList();

            if (currentSplit.Count != 4)
            {
                throw new ArgumentException("Invalid current version string", nameof(current));
            }

            if (currentSplit.Count != 4)
            {
                throw new ArgumentException("Invalid latest version string", nameof(latest));
            }

            for (var i = 0; i < currentSplit.Count; i++)
            {
                if (latestSplit[i] > currentSplit[i])
                {
                    return false;
                }

                if (latestSplit[i] < currentSplit[i])
                {
                    return true;
                }
            }

            return true;
        }

        private static async Task<Release> GetLatest()
        {
            var client = new GitHubClient(new ProductHeaderValue("spcode-client"));
            var releases = await client.Repository.Release.GetAll("Hexer10", "SPCode");
            return releases[0];
        }
    }
}