using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SPCode.Interop;
using SPCode.Utils;

namespace SPCode.UI.Windows
{
    /// <summary>
    ///     Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class ConfigWindow
    {
        private bool AllowChange;
        private bool NeedsSMDefInvalidation;

        private ICommand textBoxButtonFileCmd;

        private ICommand textBoxButtonFolderCmd;

        public ConfigWindow()
        {
            InitializeComponent();
            Language_Translate();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            foreach (var config in Program.Configs)
            {
                ConfigListBox.Items.Add(new ListBoxItem { Content = config.Name });
            }

            ConfigListBox.SelectedIndex = Program.SelectedConfig;
        }

        public ICommand TextBoxButtonFolderCmd
        {
            set { }
            get
            {
                if (textBoxButtonFolderCmd == null)
                {
                    var cmd = new SimpleCommand
                    {
                        CanExecutePredicate = o => true,
                        ExecuteAction = o =>
                        {
                            if (o is TextBox box)
                            {
                                var dialog = new CommonOpenFileDialog
                                {
                                    IsFolderPicker = true
                                };
                                var result = dialog.ShowDialog();

                                if (result == CommonFileDialogResult.Ok)
                                {
                                    box.Text = dialog.FileName;
                                }
                            }
                        }
                    };
                    textBoxButtonFolderCmd = cmd;
                    return cmd;
                }

                return textBoxButtonFolderCmd;
            }
        }

        public ICommand TextBoxButtonFileCmd
        {
            set { }
            get
            {
                if (textBoxButtonFileCmd == null)
                {
                    var cmd = new SimpleCommand
                    {
                        CanExecutePredicate = o => true,
                        ExecuteAction = o =>
                        {
                            if (o is TextBox box)
                            {
                                var dialog = new OpenFileDialog
                                {
                                    Filter = "Executables *.exe|*.exe|All Files *.*|*.*",
                                    Multiselect = false,
                                    CheckFileExists = true,
                                    CheckPathExists = true,
                                    Title = Program.Translations.GetLanguage("SelectExe")
                                };
                                var result = dialog.ShowDialog();

                                Debug.Assert(result != null, nameof(result) + " != null");
                                if (result.Value)
                                {
                                    var fInfo = new FileInfo(dialog.FileName);
                                    if (fInfo.Exists)
                                    {
                                        box.Text = fInfo.FullName;
                                    }
                                }
                            }
                        }
                    };
                    textBoxButtonFileCmd = cmd;
                    return cmd;
                }

                return textBoxButtonFileCmd;
            }
        }

        private void ConfigListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadConfigToUI(ConfigListBox.SelectedIndex);
        }

        private void LoadConfigToUI(int index)
        {
            if (index < 0 || index >= Program.Configs.Length)
            {
                return;
            }

            AllowChange = false;
            var c = Program.Configs[index];

            C_Name.Text = c.Name;
            C_SMDir.ItemsSource = c.SMDirectories;
            C_AutoCopy.IsChecked = c.AutoCopy;
            C_AutoUpload.IsChecked = c.AutoUpload;
            C_AutoRCON.IsChecked = c.AutoRCON;
            C_CopyDir.Text = c.CopyDirectory;
            C_ServerFile.Text = c.ServerFile;
            C_ServerArgs.Text = c.ServerArgs;
            C_PreBuildCmd.Text = c.PreCmd;
            C_PostBuildCmd.Text = c.PostCmd;
            C_OptimizationLevel.Value = c.OptimizeLevel;
            C_VerboseLevel.Value = c.VerboseLevel;
            C_DeleteAfterCopy.IsChecked = c.DeleteAfterCopy;
            C_FTPHost.Text = c.FTPHost;
            C_FTPUser.Text = c.FTPUser;
            C_FTPPW.Password = c.FTPPassword;
            C_FTPDir.Text = c.FTPDir;
            C_RConEngine.SelectedIndex = c.RConUseSourceEngine ? 0 : 1;
            C_RConIP.Text = c.RConIP;
            C_RConPort.Text = c.RConPort.ToString();
            C_RConPW.Password = c.RConPassword;
            C_RConCmds.Text = c.RConCommands;
            AllowChange = true;
        }

        private void NewButton_Clicked(object sender, RoutedEventArgs e)
        {
            var c = new Config
            {
                Name = "New Config",
                Standard = false,
                OptimizeLevel = 2,
                VerboseLevel = 1,
                SMDirectories = new List<string>()
            };
            var configList = new List<Config>(Program.Configs) { c };
            Program.Configs = configList.ToArray();
            ConfigListBox.Items.Add(new ListBoxItem { Content = Program.Translations.GetLanguage("NewConfig") });
        }

        private void DeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            var index = ConfigListBox.SelectedIndex;
            var c = Program.Configs[index];
            if (c.Standard)
            {
                this.ShowMessageAsync(Program.Translations.GetLanguage("CannotDelConf"),
                    Program.Translations.GetLanguage("YCannotDelConf"), MessageDialogStyle.Affirmative,
                    MetroDialogOptions);
                return;
            }

            var configList = new List<Config>(Program.Configs);
            configList.RemoveAt(index);
            Program.Configs = configList.ToArray();
            ConfigListBox.Items.RemoveAt(index);
            if (index == Program.SelectedConfig)
            {
                Program.SelectedConfig = 0;
            }

            ConfigListBox.SelectedIndex = 0;
        }

        private void AddSMDirButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var c = Program.Configs[ConfigListBox.SelectedIndex];
                if (c.SMDirectories.Contains(dialog.FileName))
                {
                    return;
                }
                c.SMDirectories.Add(dialog.FileName);
                C_SMDir.Items.Refresh();
                NeedsSMDefInvalidation = true;
            }
        }

        private void RemoveSMDirButton_Click(object sender, RoutedEventArgs e)
        {
            var c = Program.Configs[ConfigListBox.SelectedIndex];
            if (C_SMDir.SelectedItem == null)
            {
                return;
            }
            c.SMDirectories.Remove(C_SMDir.SelectedItem.ToString());
            C_SMDir.Items.Refresh();
            NeedsSMDefInvalidation = true;
        }

        private void C_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            var Name = C_Name.Text;
            Program.Configs[ConfigListBox.SelectedIndex].Name = Name;
            ((ListBoxItem)ConfigListBox.SelectedItem).Content = Name;
        }

        private void C_CopyDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].CopyDirectory = C_CopyDir.Text;
        }

        private void C_ServerFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].ServerFile = C_ServerFile.Text;
        }

        private void C_ServerArgs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].ServerArgs = C_ServerArgs.Text;
        }

        private void C_PostBuildCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].PostCmd = C_PostBuildCmd.Text;
        }

        private void C_PreBuildCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].PreCmd = C_PreBuildCmd.Text;
        }

        private void C_OptimizationLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].OptimizeLevel = (int)C_OptimizationLevel.Value;
        }

        private void C_VerboseLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].VerboseLevel = (int)C_VerboseLevel.Value;
        }

        private void C_AutoCopy_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoCopy.IsChecked != null, "C_AutoCopy.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoCopy = C_AutoCopy.IsChecked.Value;
        }

        public void C_AutoUpload_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoUpload.IsChecked != null, "C_AutoUpload.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoUpload = C_AutoUpload.IsChecked.Value;
        }

        public void C_AutoRCON_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoUpload.IsChecked != null, "C_AutoUpload.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoRCON = C_AutoRCON.IsChecked.Value;
        }

        private void C_DeleteAfterCopy_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_DeleteAfterCopy.IsChecked != null, "C_DeleteAfterCopy.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].DeleteAfterCopy = C_DeleteAfterCopy.IsChecked.Value;
        }

        private void C_FTPHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPHost = C_FTPHost.Text;
        }

        private void C_FTPUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPUser = C_FTPUser.Text;
        }

        private void C_FTPPW_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPPassword = C_FTPPW.Password;
        }

        private void C_FTPDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPDir = C_FTPDir.Text;
        }

        private void C_RConEngine_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            if (ConfigListBox.SelectedIndex >= 0)
            {
                Program.Configs[ConfigListBox.SelectedIndex].RConUseSourceEngine = C_RConEngine.SelectedIndex == 0;
            }
        }

        private void C_RConIP_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConIP = C_RConIP.Text;
        }

        private void C_RConPort_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            if (!ushort.TryParse(C_RConPort.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var newPort))
            {
                newPort = 27015;
                C_RConPort.Text = "27015";
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConPort = newPort;
        }

        private void C_RConPW_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConPassword = C_RConPW.Password;
        }

        private void C_RConCmds_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConCommands = C_RConCmds.Text;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // TODO: find out what is this for
            if (NeedsSMDefInvalidation)
            {
                foreach (var config in Program.Configs)
                {
                    config.InvalidateSMDef();
                }
            }

            // Fill a list with all configs from the ListBox

            var configsList = new List<string>();

            foreach (ListBoxItem item in ConfigListBox.Items)
            {
                configsList.Add(item.Content.ToString());
            }

            // Check for empty named configs and disallow saving configs

            foreach (var cfg in configsList)
            {
                if (cfg == string.Empty)
                {
                    e.Cancel = true;
                    this.ShowMessageAsync(Program.Translations.GetLanguage("ErrorSavingConfigs"),
                        Program.Translations.GetLanguage("EmptyConfigNames"), MessageDialogStyle.Affirmative,
                        MetroDialogOptions);
                    return;
                }
            }

            // Check for duplicate names in the config list and disallow saving configs

            if (configsList.Count != configsList.Distinct().Count())
            {
                e.Cancel = true;
                this.ShowMessageAsync(Program.Translations.GetLanguage("ErrorSavingConfigs"),
                    Program.Translations.GetLanguage("DuplicateConfigNames"), MessageDialogStyle.Affirmative,
                    MetroDialogOptions);
                return;
            }

            Program.MainWindow.FillConfigMenu();
            Program.MainWindow.ChangeConfig(Program.SelectedConfig);
            var outString = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true
            };
            using (var writer = XmlWriter.Create(outString, settings))
            {
                writer.WriteStartElement("Configurations");
                foreach (var c in Program.Configs)
                {
                    writer.WriteStartElement("Config");
                    writer.WriteAttributeString("Name", c.Name);
                    var SMDirOut = new StringBuilder();
                    foreach (var dir in c.SMDirectories)
                    {
                        SMDirOut.Append(dir.Trim() + ";");
                    }

                    writer.WriteAttributeString("SMDirectory", SMDirOut.ToString());
                    writer.WriteAttributeString("Standard", c.Standard ? "1" : "0");
                    writer.WriteAttributeString("CopyDirectory", c.CopyDirectory);
                    writer.WriteAttributeString("AutoCopy", c.AutoCopy ? "1" : "0");
                    writer.WriteAttributeString("AutoUpload", c.AutoUpload ? "1" : "0");
                    writer.WriteAttributeString("AutoRCON", c.AutoRCON ? "1" : "0");
                    writer.WriteAttributeString("ServerFile", c.ServerFile);
                    writer.WriteAttributeString("ServerArgs", c.ServerArgs);
                    writer.WriteAttributeString("PostCmd", c.PostCmd);
                    writer.WriteAttributeString("PreCmd", c.PreCmd);
                    writer.WriteAttributeString("OptimizationLevel", c.OptimizeLevel.ToString());
                    writer.WriteAttributeString("VerboseLevel", c.VerboseLevel.ToString());
                    writer.WriteAttributeString("DeleteAfterCopy", c.DeleteAfterCopy ? "1" : "0");
                    writer.WriteAttributeString("FTPHost", c.FTPHost);
                    writer.WriteAttributeString("FTPUser", c.FTPUser);
                    writer.WriteAttributeString("FTPPassword", ManagedAES.Encrypt(c.FTPPassword));
                    writer.WriteAttributeString("FTPDir", c.FTPDir);
                    writer.WriteAttributeString("RConSourceEngine", c.RConUseSourceEngine ? "1" : "0");
                    writer.WriteAttributeString("RConIP", c.RConIP);
                    writer.WriteAttributeString("RConPort", c.RConPort.ToString());
                    writer.WriteAttributeString("RConPassword", ManagedAES.Encrypt(c.RConPassword));
                    writer.WriteAttributeString("RConCommands", c.RConCommands);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.Flush();
            }

            File.WriteAllText(Paths.GetConfigFilePath(), outString.ToString());
        }

        private void Language_Translate()
        {
            if (Program.Translations.IsDefault)
            {
                return;
            }

            NewButton.Content = Program.Translations.GetLanguage("New");
            DeleteButton.Content = Program.Translations.GetLanguage("Delete");
            NameBlock.Text = Program.Translations.GetLanguage("Name");
            ScriptingDirBlock.Text = Program.Translations.GetLanguage("ScriptDir");
            CopyDirBlock.Text = Program.Translations.GetLanguage("CopyDir");
            ServerExeBlock.Text = Program.Translations.GetLanguage("ServerExe");
            ServerStartArgBlock.Text = Program.Translations.GetLanguage("serverStartArgs");
            PreBuildBlock.Text = Program.Translations.GetLanguage("PreBuildCom");
            PostBuildBlock.Text = Program.Translations.GetLanguage("PostBuildCom");
            OptimizeBlock.Text = Program.Translations.GetLanguage("OptimizeLvl");
            VerboseBlock.Text = Program.Translations.GetLanguage("VerboseLvl");
            C_AutoCopy.Content = Program.Translations.GetLanguage("AutoCopy");
            C_AutoUpload.Content = Program.Translations.GetLanguage("AutoUpload");
            C_AutoRCON.Content = Program.Translations.GetLanguage("AutoRCON");
            C_DeleteAfterCopy.Content = Program.Translations.GetLanguage("DeleteOldSMX");
            FTPHostBlock.Text = Program.Translations.GetLanguage("FTPHost");
            FTPUserBlock.Text = Program.Translations.GetLanguage("FTPUser");
            FTPPWBlock.Text = Program.Translations.GetLanguage("FTPPw");
            FTPDirBlock.Text = Program.Translations.GetLanguage("FTPDir");
            CMD_ItemC.Text = Program.Translations.GetLanguage("CMDLineCom");
            ItemC_EditorDir.Content = "{editordir} - " + Program.Translations.GetLanguage("ComEditorDir");
            ItemC_ScriptDir.Content = "{scriptdir} - " + Program.Translations.GetLanguage("ComScriptDir");
            ItemC_CopyDir.Content = "{copydir} - " + Program.Translations.GetLanguage("ComCopyDir");
            ItemC_ScriptFile.Content = "{scriptfile} - " + Program.Translations.GetLanguage("ComScriptFile");
            ItemC_ScriptName.Content = "{scriptname} - " + Program.Translations.GetLanguage("ComScriptName");
            ItemC_PluginFile.Content = "{pluginfile} - " + Program.Translations.GetLanguage("ComPluginFile");
            ItemC_PluginName.Content = "{pluginname} - " + Program.Translations.GetLanguage("ComPluginName");
            RConEngineBlock.Text = Program.Translations.GetLanguage("RConEngine");
            RConIPBlock.Text = Program.Translations.GetLanguage("RConIP");
            RConPortBlock.Text = Program.Translations.GetLanguage("RconPort");
            RConPWBlock.Text = Program.Translations.GetLanguage("RconPw");
            RConComBlock.Text = Program.Translations.GetLanguage("RconCom");
            Rcon_MenuC.Text = Program.Translations.GetLanguage("RConCMDLineCom");
            MenuC_PluginsReload.Content = "{plugins_reload} - " + Program.Translations.GetLanguage("ComPluginsReload");
            MenuC_PluginsLoad.Content = "{plugins_load} - " + Program.Translations.GetLanguage("ComPluginsLoad");
            MenuC_PluginsUnload.Content = "{plugins_unload} - " + Program.Translations.GetLanguage("ComPluginsUnload");
        }


        private class SimpleCommand : ICommand
        {
            public Predicate<object> CanExecutePredicate { get; set; }
            public Action<object> ExecuteAction { get; set; }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public void Execute(object parameter)
            {
                ExecuteAction?.Invoke(parameter);
            }
        }
    }
}