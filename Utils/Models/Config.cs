﻿using System;
using System.Collections.Generic;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.Utils
{
    public class Config
    {
        public bool AutoCopy;
        public bool AutoUpload;
        public bool AutoRCON;

        public string CopyDirectory = string.Empty;

        public bool DeleteAfterCopy;
        public string FTPDir = string.Empty;

        public string FTPHost = "ftp://localhost/";

        // securestring? No! Because it's saved in plaintext and if you want to keep it a secret, you shouldn't automatically upload it anyways...
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

        public bool RConUseSourceEngine = true;
        public string ServerArgs = string.Empty;
        public string ServerFile = string.Empty;

        private SMDefinition SMDef;

        public List<string> SMDirectories;

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