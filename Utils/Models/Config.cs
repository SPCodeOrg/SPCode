using System;
using System.Collections.Generic;
using System.Linq;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.Utils
{
    public class Config : ICloneable
    {
        public bool AutoCopy;
        public bool AutoUpload;
        public bool AutoRCON;
        public string CopyDirectory = string.Empty;
        public bool DeleteAfterCopy;
        public string FTPDir = string.Empty;
        public string FTPHost = "ftp://localhost/";
        public string FTPPassword = string.Empty;
        public string FTPUser = string.Empty;
        public string Name = string.Empty;
        public int OptimizeLevel = 2;
        public string PostCmd = string.Empty;
        public string PreCmd = string.Empty;
        public string RConCommands = string.Empty;
        public string RConIP = "127.0.0.1";
        public string RConPassword = string.Empty;
        public ushort RConPort = 27015;
        public string ServerArgs = string.Empty;
        public string ServerFile = string.Empty;

        private SMDefinition SMDef;

        public List<string> SMDirectories;
        public List<string> RejectedPaths = new();

        public bool Standard;
        public int VerboseLevel = 1;

        public SMDefinition GetSMDef()
        {
            if (SMDef == null)
            {
                LoadSMDef();
            }

            return SMDef;
        }

        public void InvalidateSMDef()
        {
            SMDef = null;
        }

        public void LoadSMDef()
        {
            if (SMDef != null)
            {
                return;
            }

            try
            {
                var def = new SMDefinition();
                def.AppendFiles(SMDirectories, out var rejectedPaths);

                RejectedPaths.Clear();

                if (rejectedPaths.Any())
                {
                    rejectedPaths.ForEach(x => RejectedPaths.Add(x));
                }

                SMDef = def;
            }
            catch (Exception)
            {
                SMDef = new SMDefinition();
            }
        }

        public object Clone()
        {
            return new Config
            {
                AutoCopy = AutoCopy,
                AutoUpload = AutoUpload,
                AutoRCON = AutoRCON,
                CopyDirectory = CopyDirectory,
                DeleteAfterCopy = DeleteAfterCopy,
                FTPDir = FTPDir,
                FTPHost = FTPHost,
                FTPPassword = FTPPassword,
                FTPUser = FTPUser,
                Name = Name,
                OptimizeLevel = OptimizeLevel,
                PostCmd = PostCmd,
                PreCmd = PreCmd,
                RConCommands = RConCommands,
                RConIP = RConIP,
                RConPort = RConPort,
                RConPassword = RConPassword,
                ServerArgs = ServerArgs,
                ServerFile = ServerFile,
                SMDef = SMDef,
                SMDirectories = SMDirectories,
                RejectedPaths = RejectedPaths,
                Standard = Standard,
                VerboseLevel = VerboseLevel
            };
        }
    }
}