using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiscordRPC;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using SPCode.UI.Components;

namespace SPCode.UI.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class OptionsWindow
    {
        private readonly string[] AvailableAccents =
        {
            "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet",
            "Pink", "Magenta", "Crimson", "Amber",
            "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
        };
        private bool RestartTextIsShown;
        private readonly bool AllowChanging;

        public OptionsWindow()
        {
            InitializeComponent();
            Language_Translate();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            LoadSettings();
            AllowChanging = true;
        }

        private async void RestoreButton_Clicked(object sender, RoutedEventArgs e)
        {
            var result = await this.ShowMessageAsync(Program.Translations.GetLanguage("ResetOptions"),
                Program.Translations.GetLanguage("ResetOptQues"), MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                Program.OptionsObject = new OptionsControl();
                Program.OptionsObject.ReCreateCryptoKey();
                Program.MainWindow.OptionMenuEntry.IsEnabled = false;
                await this.ShowMessageAsync(Program.Translations.GetLanguage("RestartEditor"),
                    Program.Translations.GetLanguage("YRestartEditor"), MessageDialogStyle.Affirmative,
                    MetroDialogOptions);
                Close();
            }
        }

        private void HardwareAcc_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(HardwareAcc.IsChecked != null, "HardwareAcc.IsChecked != null");
            Program.OptionsObject.Program_UseHardwareAcceleration = HardwareAcc.IsChecked.Value;
            ToggleRestartText();
        }

        private void UIAnimation_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(UIAnimation.IsChecked != null, "UIAnimation.IsChecked != null");
            Program.OptionsObject.UI_Animations = UIAnimation.IsChecked.Value;
            ToggleRestartText();
        }

        private void AutoOpenInclude_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(OpenIncludes.IsChecked != null, "OpenIncludes.IsChecked != null");
            Program.OptionsObject.Program_OpenCustomIncludes = OpenIncludes.IsChecked.Value;
            OpenIncludesRecursive.IsEnabled = OpenIncludes.IsChecked.Value;
        }

        private void OpenIncludeRecursively_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(OpenIncludesRecursive.IsChecked != null, "OpenIncludesRecursive.IsChecked != null");
            Program.OptionsObject.Program_OpenIncludesRecursively = OpenIncludesRecursive.IsChecked.Value;
        }

        private void AutoUpdate_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(AutoUpdate.IsChecked != null, "AutoUpdate.IsChecked != null");
            Program.OptionsObject.Program_CheckForUpdates = AutoUpdate.IsChecked.Value;
        }

        private void ShowToolbar_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(ShowToolBar.IsChecked != null, "ShowToolBar.IsChecked != null");
            Program.OptionsObject.UI_ShowToolBar = ShowToolBar.IsChecked.Value;
            Program.MainWindow.Win_ToolBar.Height = Program.OptionsObject.UI_ShowToolBar
                ? double.NaN
                : 0.0;
        }

        private void DynamicISAC_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(DynamicISAC.IsChecked != null, "DynamicISAC.IsChecked != null");
            Program.OptionsObject.Program_DynamicISAC = DynamicISAC.IsChecked.Value;
        }

        private void DarkTheme_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(DarkTheme.IsChecked != null, "DarkTheme.IsChecked != null");
            Program.OptionsObject.Program_Theme = DarkTheme.IsChecked.Value ? "BaseDark" : "BaseLight";
            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            ThemeManager.ChangeAppStyle(Program.MainWindow,
                ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            ToggleRestartText(true);
        }

        private void AccentColor_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Program.OptionsObject.Program_AccentColor = (string)AccentColor.SelectedItem;
            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            ThemeManager.ChangeAppStyle(Program.MainWindow,
                ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
        }

        private void FontSize_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            var size = FontSizeD.Value;
            Program.OptionsObject.Editor_FontSize = size;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.UpdateFontSize(size);
                }
            }
        }

        private void ScrollSpeed_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Program.OptionsObject.Editor_ScrollLines = ScrollSpeed.Value;
        }

        private void WordWrap_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(WordWrap.IsChecked != null, "WordWrap.IsChecked != null");
            var wrapping = WordWrap.IsChecked.Value;
            Program.OptionsObject.Editor_WordWrap = wrapping;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.WordWrap = wrapping;
                }
            }
        }

        private void AIndentation_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(AgressiveIndentation.IsChecked != null, "AgressiveIndentation.IsChecked != null");
            Program.OptionsObject.Editor_AgressiveIndentation = AgressiveIndentation.IsChecked.Value;
        }

        private void LineReformat_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(LineReformatting.IsChecked != null, "LineReformatting.IsChecked != null");
            Program.OptionsObject.Editor_ReformatLineAfterSemicolon = LineReformatting.IsChecked.Value;
        }

        private void TabToSpace_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(TabToSpace.IsChecked != null, "TabToSpace.IsChecked != null");
            var replaceTabs = TabToSpace.IsChecked.Value;
            Program.OptionsObject.Editor_ReplaceTabsToWhitespace = replaceTabs;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.Options.ConvertTabsToSpaces = replaceTabs;
                }
            }
        }

        private void AutoCloseBrackets_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(AutoCloseBrackets.IsChecked != null, "AutoCloseBrackets.IsChecked != null");
            Program.OptionsObject.Editor_AutoCloseBrackets = AutoCloseBrackets.IsChecked.Value;
        }

        private void AutoCloseStringChars_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(AutoCloseStringChars.IsChecked != null, "AutoCloseStringChars.IsChecked != null");
            Program.OptionsObject.Editor_AutoCloseStringChars = AutoCloseStringChars.IsChecked.Value;
        }

        private void ShowSpaces_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(ShowSpaces.IsChecked != null, "ShowSpaces.IsChecked != null");
            var showSpacesValue = Program.OptionsObject.Editor_ShowSpaces = ShowSpaces.IsChecked.Value;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.Options.ShowSpaces = showSpacesValue;
                }
            }
        }

        private void ShowTabs_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(ShowTabs.IsChecked != null, "ShowTabs.IsChecked != null");
            var showTabsValue = Program.OptionsObject.Editor_ShowTabs = ShowTabs.IsChecked.Value;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.Options.ShowTabs = showTabsValue;
                }
            }
        }

        private void FontFamily_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            var family = (FontFamily)FontFamilyCB.SelectedItem;
            var FamilyString = family.Source;
            Program.OptionsObject.Editor_FontFamily = FamilyString;
            FontFamilyTB.Text = "Font (" + FamilyString + "):";
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.FontFamily = family;
                }
            }
        }

        private void IndentationSize_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            var indentationSizeValue =
                Program.OptionsObject.Editor_IndentationSize = (int)Math.Round(IndentationSize.Value);
            var editors = Program.MainWindow.GetAllEditorElements();
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    editor.editor.Options.IndentationSize = indentationSizeValue;
                }
            }
        }


        private void HighlightDeprecateds_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(HighlightDeprecateds.IsChecked != null, "HighlightDeprecateds.IsChecked != null");
            Program.OptionsObject.SH_HighlightDeprecateds = HighlightDeprecateds.IsChecked.Value;
            ToggleRestartText();
        }

        private void LanguageBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            var originalSource = (ComboBox)e.OriginalSource;
            var selectedItem = (ComboboxItem)originalSource.SelectedItem;
            var lang = Program.Translations.AvailableLanguageIDs.FirstOrDefault(l => l == selectedItem.Value);

            Program.Translations.LoadLanguage(lang);
            Program.OptionsObject.Language = lang;
            Program.MainWindow.Language_Translate();
            Language_Translate();
            ToggleRestartText(true);
        }

        private void HardwareSalts_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(HardwareSalts.IsChecked != null, "HardwareSalts.IsChecked != null");
            Program.OptionsObject.Program_UseHardwareSalts = HardwareSalts.IsChecked.Value;
            Program.RCCKMade = false;
            Program.OptionsObject.ReCreateCryptoKey();
            Program.MakeRCCKAlert();
        }

        private void DiscordPresence_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            Debug.Assert(DiscordPresence.IsChecked != null, "DiscordPresence.IsChecked != null");
            var val = DiscordPresence.IsChecked.Value;
            Program.OptionsObject.Program_DiscordPresence = val;
            if (val && !Program.discordClient.IsInitialized)
            {
                Program.discordClient = new DiscordRpcClient("692110664948514836");
                Program.discordTime = Timestamps.Now;

                // Init Discord RPC
                Program.discordClient.Initialize();

                Program.MainWindow.UpdateWindowTitle();
            }
            else if (!val && Program.discordClient.IsInitialized)
            {
                Program.discordClient.Dispose();
            }
        }

        private void AutoSave_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging)
            {
                return;
            }

            var newIndex = AutoSave.SelectedIndex;
            var editors = Program.MainWindow.GetAllEditorElements();
            if (newIndex == 0)
            {
                Program.OptionsObject.Editor_AutoSave = false;
                if (editors != null)
                {
                    foreach (var editor in editors)
                    {
                        if (editor.AutoSaveTimer.Enabled)
                        {
                            editor.AutoSaveTimer.Stop();
                        }
                    }
                }
            }
            else
            {
                Program.OptionsObject.Editor_AutoSave = true;
                if (newIndex == 1)
                {
                    Program.OptionsObject.Editor_AutoSaveInterval = 30;
                }
                else if (newIndex == 7)
                {
                    Program.OptionsObject.Editor_AutoSaveInterval = 600;
                }
                else if (newIndex == 8)
                {
                    Program.OptionsObject.Editor_AutoSaveInterval = 900;
                }
                else
                {
                    Program.OptionsObject.Editor_AutoSaveInterval = (newIndex - 1) * 60;
                }

                if (editors != null)
                {
                    foreach (var editor in editors)
                    {
                        editor.StartAutoSaveTimer();
                    }
                }
            }
        }

        private void LoadSettings()
        {
            foreach (var accent in AvailableAccents)
            {
                AccentColor.Items.Add(accent);
            }

            HardwareAcc.IsChecked = Program.OptionsObject.Program_UseHardwareAcceleration;
            UIAnimation.IsChecked = Program.OptionsObject.UI_Animations;
            OpenIncludes.IsChecked = Program.OptionsObject.Program_OpenCustomIncludes;
            OpenIncludesRecursive.IsChecked = Program.OptionsObject.Program_OpenIncludesRecursively;
            AutoUpdate.IsChecked = Program.OptionsObject.Program_CheckForUpdates;
            if (!Program.OptionsObject.Program_OpenCustomIncludes)
            {
                OpenIncludesRecursive.IsEnabled = false;
            }

            ShowToolBar.IsChecked = Program.OptionsObject.UI_ShowToolBar;
            DynamicISAC.IsChecked = Program.OptionsObject.Program_DynamicISAC;
            DarkTheme.IsChecked = Program.OptionsObject.Program_Theme == "BaseDark";
            for (var i = 0; i < AvailableAccents.Length; ++i)
            {
                if (AvailableAccents[i] == Program.OptionsObject.Program_AccentColor)
                {
                    AccentColor.SelectedIndex = i;
                    break;
                }
            }

            for (var i = 0; i < Program.Translations.AvailableLanguages.Length; ++i)
            {
                var item = new ComboboxItem
                {
                    Text = Program.Translations.AvailableLanguages[i],
                    Value = Program.Translations.AvailableLanguageIDs[i]
                };
                LanguageBox.Items.Add(item);
                if (Program.OptionsObject.Language == Program.Translations.AvailableLanguageIDs[i])
                {
                    LanguageBox.SelectedIndex = i;
                }
            }

            if (Program.OptionsObject.Editor_AutoSave)
            {
                var seconds = Program.OptionsObject.Editor_AutoSaveInterval;
                if (seconds < 60)
                {
                    AutoSave.SelectedIndex = 1;
                }
                else if (seconds <= 300)
                {
                    AutoSave.SelectedIndex = Math.Max(1, Math.Min(seconds / 60, 5)) + 1;
                }
                else if (seconds == 600)
                {
                    AutoSave.SelectedIndex = 7;
                }
                else
                {
                    AutoSave.SelectedIndex = 8;
                }
            }
            else
            {
                AutoSave.SelectedIndex = 0;
            }

            HighlightDeprecateds.IsChecked = Program.OptionsObject.SH_HighlightDeprecateds;
            FontSizeD.Value = Program.OptionsObject.Editor_FontSize;
            ScrollSpeed.Value = Program.OptionsObject.Editor_ScrollLines;
            WordWrap.IsChecked = Program.OptionsObject.Editor_WordWrap;
            AgressiveIndentation.IsChecked = Program.OptionsObject.Editor_AgressiveIndentation;
            LineReformatting.IsChecked = Program.OptionsObject.Editor_ReformatLineAfterSemicolon;
            TabToSpace.IsChecked = Program.OptionsObject.Editor_ReplaceTabsToWhitespace;
            AutoCloseBrackets.IsChecked = Program.OptionsObject.Editor_AutoCloseBrackets;
            AutoCloseStringChars.IsChecked = Program.OptionsObject.Editor_AutoCloseStringChars;
            ShowSpaces.IsChecked = Program.OptionsObject.Editor_ShowSpaces;
            ShowTabs.IsChecked = Program.OptionsObject.Editor_ShowTabs;
            FontFamilyTB.Text =
                $"{Program.Translations.GetLanguage("FontFamily")} ({Program.OptionsObject.Editor_FontFamily}):";
            FontFamilyCB.SelectedValue = new FontFamily(Program.OptionsObject.Editor_FontFamily);
            IndentationSize.Value = Program.OptionsObject.Editor_IndentationSize;
            HardwareSalts.IsChecked = Program.OptionsObject.Program_UseHardwareSalts;
            DiscordPresence.IsChecked = Program.OptionsObject.Program_DiscordPresence;
            LoadSH();
        }

        private void ToggleRestartText(bool FullEffect = false)
        {
            if (AllowChanging)
            {
                if (!RestartTextIsShown)
                {
                    StatusTextBlock.Content = FullEffect
                        ? Program.Translations.GetLanguage("RestartEdiFullEff")
                        : Program.Translations.GetLanguage("RestartEdiEff");
                    RestartTextIsShown = true;
                }
            }
        }

        private void Language_Translate()
        {
            if (Program.Translations.IsDefault)
            {
                return;
            }

            ResetButton.Content = Program.Translations.GetLanguage("ResetOptions");
            ProgramHeader.Header = $" {Program.Translations.GetLanguage("Program")}";

            HardwareAcc.Content = Program.Translations.GetLanguage("HardwareAcc");
            UIAnimation.Content = Program.Translations.GetLanguage("UIAnim");
            OpenIncludes.Content = Program.Translations.GetLanguage("OpenInc");
            OpenIncludesRecursive.Content = Program.Translations.GetLanguage("OpenIncRec");
            AutoUpdate.Content = Program.Translations.GetLanguage("AutoUpdate");
            ShowToolBar.Content = Program.Translations.GetLanguage("ShowToolbar");
            DynamicISAC.Content = Program.Translations.GetLanguage("DynamicISAC");
            DarkTheme.Content = Program.Translations.GetLanguage("DarkTheme");
            ThemeColorLabel.Content = Program.Translations.GetLanguage("ThemeColor");
            LanguageLabel.Content = Program.Translations.GetLanguage("LanguageStr");
            EditorHeader.Header = $" {Program.Translations.GetLanguage("Editor")}";
            FontSizeBlock.Text = Program.Translations.GetLanguage("FontSize");
            ScrollSpeedBlock.Text = Program.Translations.GetLanguage("ScrollSpeed");
            WordWrap.Content = Program.Translations.GetLanguage("WordWrap");
            AgressiveIndentation.Content = Program.Translations.GetLanguage("AggIndentation");
            LineReformatting.Content = Program.Translations.GetLanguage("ReformatAfterSem");
            TabToSpace.Content = Program.Translations.GetLanguage("TabsToSpace");
            AutoCloseBrackets.Content = $"{Program.Translations.GetLanguage("AutoCloseBrack")} (), [], {{}}";
            AutoCloseStringChars.Content = $"{Program.Translations.GetLanguage("AutoCloseStrChr")} \"\", ''";
            ShowSpaces.Content = Program.Translations.GetLanguage("ShowSpaces");
            ShowTabs.Content = Program.Translations.GetLanguage("ShowTabs");
            IndentationSizeBlock.Text = Program.Translations.GetLanguage("IndentationSize");
            SyntaxHighBlock.Text = Program.Translations.GetLanguage("SyntaxHigh");
            HighlightDeprecateds.Content = Program.Translations.GetLanguage("HighDeprecat");
            AutoSaveBlock.Text = Program.Translations.GetLanguage("AutoSaveMin");
            DefaultButton.Content = Program.Translations.GetLanguage("DefaultValues");
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            Program.OptionsObject.NormalizeSHColors();
            LoadSH();
        }
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}