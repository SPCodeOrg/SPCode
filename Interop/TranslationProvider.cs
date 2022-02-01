using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml;
using Octokit;
using SPCode.Utils;

namespace SPCode.Interop
{
    public class TranslationProvider
    {
        public List<string> AvailableLanguageIDs = new();
        public List<string> AvailableLanguages = new();
        private readonly string _tempDir = Paths.GetTempDirectory();
        private readonly string _translationsDir = Paths.GetTranslationsDirectory();

        public bool IsDefault = true;

        private readonly Dictionary<string, string> LangDict = new(StringComparer.OrdinalIgnoreCase);

        public TranslationProvider()
        {
            // Make sure translations dir exists
            if (!Directory.Exists(_translationsDir))
            {
                Directory.CreateDirectory(_translationsDir);
            }

            if (IsUpdateAvailable(out var latestVersion))
            {
                UpdateTranslations(latestVersion);
            }

            ParseTranslationFiles();
        }

        /// <summary>
        /// Gets the translation of the specified phrase.
        /// </summary>
        /// <param name="phrase">The phrase to return translated</param>
        /// <returns></returns>
        public string Get(string phrase)
        {
            return LangDict.ContainsKey(phrase) ? LangDict[phrase] : "<empty>";
        }

        /// <summary>
        /// Loads the specified language.
        /// </summary>
        /// <param name="lang">The language to load</param>
        /// <param name="Initial"></param>
        public void LoadLanguage(string lang, bool Initial = false)
        {
            var languageList = new List<string>();
            var languageIDList = new List<string>();
            languageList.Add("English");
            languageIDList.Add("");
            lang = lang.Trim().ToLowerInvariant();
            IsDefault = (string.IsNullOrEmpty(lang) || lang.ToLowerInvariant() == "en") && Initial;
            if (File.Exists(Constants.LanguagesFile))
            {
                try
                {
                    var document = new XmlDocument();
                    document.Load(Constants.LanguagesFile);
                    if (document.ChildNodes.Count < 1)
                    {
                        throw new Exception("No Root-Node: \"translations\" found");
                    }

                    XmlNode rootLangNode = null;
                    foreach (XmlNode childNode in document.ChildNodes[0].ChildNodes)
                    {
                        var lID = childNode.Name;
                        var lNm = lID;
                        if (childNode.Name.ToLowerInvariant() == lang)
                        {
                            rootLangNode = childNode;
                        }
                        if (childNode.FirstChild.Name.ToLowerInvariant() == "language")
                        {
                            lNm = childNode.FirstChild.InnerText;
                        }
                        languageList.Add(lNm);
                        languageIDList.Add(lID);
                    }
                    if (rootLangNode != null)
                    {
                        foreach (XmlNode node in rootLangNode.ChildNodes)
                        {
                            if (node.NodeType == XmlNodeType.Comment)
                            {
                                continue;
                            }
                            var nn = node.Name.ToLowerInvariant();
                            var nv = node.InnerText;
                            LangDict[nn] = nv;
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while reading the language-file. Without them, the editor wont show translations." + Environment.NewLine + "Details: " + e.Message
                        , "Error while reading configs."
                        , MessageBoxButton.OK
                        , MessageBoxImage.Warning);
                }
            }
            AvailableLanguages = languageList;
            AvailableLanguageIDs = languageIDList;
        }

        public void ParseTranslationFiles()
        {
            try
            {
                var filesDir = Directory.GetFiles(_translationsDir).Where(x => x.EndsWith(".xml"));
                foreach (var file in filesDir)
                {
                    // Create wrapper
                    var fInfo = new FileInfo(file);

                    // Parse content in an XML object
                    var doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(fInfo.FullName));

                    // Get language name and ID
                    var langName = doc.ChildNodes[0].ChildNodes
                        .Cast<XmlNode>()
                        .Single(x => x.Name == "language")
                        .InnerText;
                    var langID = fInfo.Name.Substring(0, fInfo.Name.IndexOf('.'));

                    // Add file to the available languages lists
                    AvailableLanguages.Add(langName);
                    AvailableLanguageIDs.Add(langID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was a problem while updating the translations file.\n" +
                    $"Details: {ex.Message}");
            }
        }

        public void UpdateTranslations(Release latestVersion)
        {
            // Clear temp folder before beggining
            DirUtils.ClearTempFolder();

            // Download latest release zip file
            var wc = new WebClient();
            var downloadedFile = Path.Combine(_tempDir, "langs.zip");
            wc.Headers.Add(HttpRequestHeader.UserAgent, Constants.ProductHeaderValueName);
            wc.DownloadFile(latestVersion.ZipballUrl, downloadedFile);

            // Decompress and replace all of its files
            ZipFile.ExtractToDirectory(downloadedFile, _tempDir);
            var filesDir = Directory.GetFiles(Directory.GetDirectories(_tempDir)[0]).Where(x => x.EndsWith(".xml"));
            foreach (var file in filesDir)
            {
                // Create wrapper
                var fInfo = new FileInfo(file);

                // Replace current file with this one
                var destination = Path.Combine(_translationsDir, fInfo.Name);
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
                File.Move(fInfo.FullName, destination);
            }

            // Delete all temp folder contents
            DirUtils.ClearTempFolder();

            // Update version to options object
            Program.OptionsObject.TranslationsVersion = int.Parse(latestVersion.Name);
        }

        public bool IsUpdateAvailable(out Release latestVersion)
        {
            var client = new GitHubClient(new ProductHeaderValue(Constants.ProductHeaderValueName));

            // Check if translations need update by comparing stored version with latest release name from repository
            var versionStored = Program.OptionsObject.TranslationsVersion;

            latestVersion = client.Repository.Release.GetAll(Constants.OrgName,
                Constants.TranslationsRepoName).Result[0];

            return latestVersion != null && versionStored != 0 && versionStored < int.Parse(latestVersion.Name);
        }
    }
}