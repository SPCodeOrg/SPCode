using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml;
using SourcepawnCondenser.SourcemodDefinition;
using Spcode.Utils;

namespace Spcode.Interop
{
    public static class ConfigLoader
    {
        public static Config[] Load()
        {
            var configs = new List<Config>();
            if (File.Exists("sourcepawn\\configs\\Configs.xml"))
            {
                try
                {
                    var document = new XmlDocument();
                    document.Load("sourcepawn\\configs\\Configs.xml");
                    if (document.ChildNodes.Count < 1) throw new Exception("No main 'Configurations' node.");
                    var mainNode = document.ChildNodes[0];
                    if (mainNode.ChildNodes.Count < 1) throw new Exception("No 'config' nodes found.");
                    for (var i = 0; i < mainNode.ChildNodes.Count; ++i)
                    {
                        var node = mainNode.ChildNodes[i];
                        var _Name = ReadAttributeStringSafe(ref node, "Name", "UNKOWN CONFIG " + (i + 1));
                        var _SMDirectoryStr = ReadAttributeStringSafe(ref node, "SMDirectory");
                        var SMDirectoriesSplitted = _SMDirectoryStr.Split(';');
                        var SMDirs = new List<string>();
                        foreach (var dir in SMDirectoriesSplitted)
                        {
                            var d = dir.Trim();
                            if (Directory.Exists(d)) SMDirs.Add(d);
                        }

                        var _Standard = ReadAttributeStringSafe(ref node, "Standard", "0");
                        var _AutoCopyStr = ReadAttributeStringSafe(ref node, "AutoCopy", "0");
                        var _AutoUploadStr = ReadAttributeStringSafe(ref node, "AutoUpload", "0");
                        var _AutoRCONStr = ReadAttributeStringSafe(ref node, "AutoRCON", "0");
                        var _CopyDirectory = ReadAttributeStringSafe(ref node, "CopyDirectory");
                        var _ServerFile = ReadAttributeStringSafe(ref node, "ServerFile");
                        var _ServerArgs = ReadAttributeStringSafe(ref node, "ServerArgs");
                        var _PostCmd = ReadAttributeStringSafe(ref node, "PostCmd");
                        var _PreCmd = ReadAttributeStringSafe(ref node, "PreCmd");
                        
                        var IsStandardConfig = _Standard != "0" && !string.IsNullOrWhiteSpace(_Standard);
                        var _AutoCopy = _AutoCopyStr != "0" && !string.IsNullOrWhiteSpace(_AutoCopyStr);
                        var _AutoUpload = _AutoUploadStr != "0" && !string.IsNullOrWhiteSpace(_AutoUploadStr);
                        var _AutoRCON = _AutoRCONStr != "0" && !string.IsNullOrWhiteSpace(_AutoRCONStr);
                        
                        int _OptimizationLevel = 2, _VerboseLevel = 1;
                        int subValue;
                        if (int.TryParse(ReadAttributeStringSafe(ref node, "OptimizationLevel", "2"), out subValue))
                            _OptimizationLevel = subValue;
                        if (int.TryParse(ReadAttributeStringSafe(ref node, "VerboseLevel", "1"), out subValue))
                            _VerboseLevel = subValue;
                        var _DeleteAfterCopy = false;
                        var DeleteAfterCopyStr = ReadAttributeStringSafe(ref node, "DeleteAfterCopy", "0");
                        if (!(DeleteAfterCopyStr == "0" || string.IsNullOrWhiteSpace(DeleteAfterCopyStr)))
                            _DeleteAfterCopy = true;
                        var _FTPHost = ReadAttributeStringSafe(ref node, "FTPHost", "ftp://localhost/");
                        var _FTPUser = ReadAttributeStringSafe(ref node, "FTPUser");
                        var encryptedFTPPW = ReadAttributeStringSafe(ref node, "FTPPassword");
                        var _FTPPW = ManagedAES.Decrypt(encryptedFTPPW);
                        var _FTPDir = ReadAttributeStringSafe(ref node, "FTPDir");
                        var _RConEngineSourceStr = ReadAttributeStringSafe(ref node, "RConSourceEngine", "1");
                        bool _RConEngineTypeSource = !(_RConEngineSourceStr == "0" || string.IsNullOrWhiteSpace(_RConEngineSourceStr));
                        var _RConIP = ReadAttributeStringSafe(ref node, "RConIP", "127.0.0.1");
                        var _RConPortStr = ReadAttributeStringSafe(ref node, "RConPort", "27015");
                        ushort _RConPort = 27015;
                        if (!ushort.TryParse(_RConPortStr, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out _RConPort)) _RConPort = 27015;
                        var encryptedRConPassword = ReadAttributeStringSafe(ref node, "RConPassword");
                        var _RConPassword = ManagedAES.Decrypt(encryptedRConPassword);
                        var _RConCommands = ReadAttributeStringSafe(ref node, "RConCommands");
                        var c = new Config
                        {
                            Name = _Name,
                            SMDirectories = SMDirs.ToArray(),
                            Standard = IsStandardConfig,
                            AutoCopy = _AutoCopy,
                            AutoRCON = _AutoRCON,
                            AutoUpload = _AutoUpload,
                            CopyDirectory = _CopyDirectory,
                            ServerFile = _ServerFile,
                            ServerArgs = _ServerArgs,
                            PostCmd = _PostCmd,
                            PreCmd = _PreCmd,
                            OptimizeLevel = _OptimizationLevel,
                            VerboseLevel = _VerboseLevel,
                            DeleteAfterCopy = _DeleteAfterCopy,
                            FTPHost = _FTPHost,
                            FTPUser = _FTPUser,
                            FTPPassword = _FTPPW,
                            FTPDir = _FTPDir,
                            RConUseSourceEngine = _RConEngineTypeSource,
                            RConIP = _RConIP,
                            RConPort = _RConPort,
                            RConPassword = _RConPassword,
                            RConCommands = _RConCommands
                        };
                        if (IsStandardConfig) c.LoadSMDef();
                        configs.Add(c);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        "An error appeared while reading the configs. Without them, the editor wont start. Reinstall program!" +
                        Environment.NewLine + "Details: " + e.Message
                        , "Error while reading configs."
                        , MessageBoxButton.OK
                        , MessageBoxImage.Warning);
                    Environment.Exit(Environment.ExitCode);
                }
            }
            else
            {
                MessageBox.Show(
                    "The Editor could not find the Configs.xml file. Without it, the editor wont start. Reinstall program.",
                    "File not found.", MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(Environment.ExitCode);
            }

            return configs.ToArray();
        }

        private static string ReadAttributeStringSafe(ref XmlNode node, string attributeName, string defaultValue = "")
        {
            for (var i = 0; i < node.Attributes.Count; ++i)
                if (node.Attributes[i].Name == attributeName)
                    return node.Attributes[i].Value;
            return defaultValue;
        }
    }

    public class Config
    {
        public bool AutoCopy;
        public bool AutoUpload;
        public bool AutoRCON;
        
        public string CopyDirectory = string.Empty;

        public bool DeleteAfterCopy;
        public string FTPDir = string.Empty;

        public string FTPHost = "ftp://localhost/";

        public string
            FTPPassword =
                string.Empty; //securestring? No! Because it's saved in plaintext and if you want to keep it a secret, you shouldn't automaticly uploade it anyways...

        public string FTPUser = string.Empty;
        public string Name = string.Empty;

        public int OptimizeLevel = 2;

        public string PostCmd = string.Empty;
        public string PreCmd = string.Empty;
        public string RConCommands = string.Empty;
        public string RConIP = "127.0.0.1";
        public string RConPassword = string.Empty;
        public ushort RConPort = 27015;

        public bool RConUseSourceEngine = true;
        public string ServerArgs = string.Empty;
        public string ServerFile = string.Empty;

        private SMDefinition SMDef;

        public string[] SMDirectories = new string[0];

        public bool Standard;
        public int VerboseLevel = 1;

        public SMDefinition GetSMDef()
        {
            if (SMDef == null) LoadSMDef();
            return SMDef;
        }

        public void InvalidateSMDef()
        {
            SMDef = null;
        }

        public void LoadSMDef()
        {
            if (SMDef != null) return;
            try
            {
                var def = new SMDefinition();
                def.AppendFiles(SMDirectories);
                SMDef = def;
            }
            catch (Exception)
            {
                SMDef = new SMDefinition(); //this could be dangerous...
            }
        }
    }
}