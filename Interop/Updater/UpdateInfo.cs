using System.Linq;
using Octokit;

namespace Spcode.Interop.Updater
{
    public class UpdateInfo
    {
        public string ExceptionMessage = string.Empty;

        public bool GotException = false;

        public bool IsAvailable = false;

        public Release Release = null;

        public bool SkipDialog = false;
        public bool WriteAble = true;

        public ReleaseAsset Asset => Release.Assets.FirstOrDefault(e => e.Name == "SpcodeUpdater.exe");
    }
}