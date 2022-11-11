using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace SPCode.Interop.Updater;

public class UpdateInfo
{
    public string ExceptionMessage = string.Empty;
    public bool GotException = false;
    public bool IsAvailable = false;
    public List<Release> AllReleases;
    public bool SkipDialog = false;
    public bool WriteAble = true;
    public ReleaseAsset Updater => AllReleases[0].Assets.FirstOrDefault(e => e.Name == "SPCodeUpdater.exe");
    public ReleaseAsset Portable => AllReleases[0].Assets.FirstOrDefault(e => e.Name.Contains("Portable"));
}