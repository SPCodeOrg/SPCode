namespace SPCode.Utils;

public static class Constants
{
    #region GitHub
    public const string GitHubRepository = "https://github.com/SPCodeOrg/SPCode";
    public const string GitHubNewIssueLink = "https://github.com/SPCodeOrg/SPCode/issues/new";
    public const string GitHubReleases = "https://github.com/SPCodeOrg/SPCode/releases";
    public const string GitHubLatestRelease = "https://github.com/SPCodeOrg/SPCode/releases/latest";
    public const string GitHubWiki = "https://github.com/SPCodeOrg/SPCode/wiki";
    public const string OrgName = "SPCodeOrg";
    public const string MainRepoName = "SPCode";
    public const string TranslationsRepoName = "spcode-translations";
    public const string ProductHeaderValueName = "spcode-client";
    #endregion

    #region Icons
    public const string FolderIcon = "icon-folder.png";
    public const string IncludeIcon = "icon-include.png";
    public const string PluginIcon = "icon-plugin.png";
    public const string TxtIcon = "icon-txt.png";
    public const string SmxIcon = "icon-smx.png";
    public const string EmptyIcon = "empty-box.png";
    #endregion

    #region Files
    public const string HotkeysFile = "Hotkeys.xml";
    public const string LicenseFile = "License.txt";
    public const string LanguagesFile = "lang_0_spcode.xml";
    public const string DefaultTranslationsFile = "default.xml";
    public const string SPCompiler = "spcomp.exe";
    #endregion

    #region Filters
    public const string ErrorFilterRegex = @"^(?<File>.+?)\((?<Line>[0-9]+(\s*--\s*[0-9]+)?)\)\s*:\s*(?<Type>[a-zA-Z]+\s+([a-zA-Z]+\s+)?[0-9]+)\s*:(?<Details>.+)";
    public const string FileSaveFilters = @"Sourcepawn Files (*.sp *.inc)|*.sp;*.inc|All Files (*.*)|*.*";
    public const string FileOpenFilters = @"Sourcepawn Files (*.sp *.inc)|*.sp;*.inc|Sourcemod Plugins (*.smx)|*.smx|All Files (*.*)|*.*";
    public const string DecompileFileFilters = "Sourcepawn Plugins (*.smx)|*.smx";
    public const string DatFilesFilter = "Dat files (.dat)|*.dat";
    public const string ServerExecutableFilter = "Executables *.exe|*.exe|All Files *.*|*.*";
    #endregion

    #region Other
    public const string GetSPCodeText = "Get SPCode";
    public const string DiscordRPCAppID = "692110664948514836";
    public const string DefaultLanguageID = "default";
    #endregion
}