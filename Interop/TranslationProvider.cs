using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using SPCode.Utils;
using static SPCode.Utils.DefaultTranslations;

namespace SPCode.Interop;

public class TranslationProvider
{
    public string[] AvailableLanguageIDs;
    public string[] AvailableLanguages;

    public bool IsDefault = true;

    private readonly Dictionary<string, string> LangDict = new(StringComparer.OrdinalIgnoreCase);

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
        DefaultLangDict.Keys.ToList().ForEach(x => LangDict[x] = DefaultLangDict[x]);
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
        AvailableLanguages = languageList.ToArray();
        AvailableLanguageIDs = languageIDList.ToArray();
    }    
}
