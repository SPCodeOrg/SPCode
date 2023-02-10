using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DiscordRPC;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using SPCode.UI.Components;
using SPCode.Utils;
using SPCode.Utils.Models;
using static SPCode.Interop.TranslationProvider;
using Button = DiscordRPC.Button;

namespace SPCode.UI.Windows;

public partial class OptionsWindow
{
    #region Variables
    private readonly string[] AvailableAccents =
    {
        "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet",
        "Pink", "Magenta", "Crimson", "Amber",
        "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
    };

    private bool AllowChanging;
    #endregion

    #region Constructors
    public OptionsWindow()
    {
        InitializeComponent();

        if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
        {
            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
        }

        LoadSettings();
        LoadSH();
        LoadHotkeysSection();
        Language_Translate();
        EvaluateRTL();

        AllowChanging = true;

        SaveHotkeyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        SaveHotkeyTimer.Tick += OnTimerTick;
    }
    #endregion

    #region Events
    private void DefaultButton_Click(object sender, RoutedEventArgs e)
    {
        Program.OptionsObject.NormalizeSHColors();
        LoadSH();
    }

    private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
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
    }

    private void UIAnimation_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Debug.Assert(UIAnimation.IsChecked != null, "UIAnimation.IsChecked != null");
        Program.OptionsObject.UI_Animations = UIAnimation.IsChecked.Value;
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.UpdateFontSize(size);
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.WordWrap = wrapping;
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.Options.ConvertTabsToSpaces = replaceTabs;
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.Options.ShowSpaces = showSpacesValue;
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.Options.ShowTabs = showTabsValue;
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
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.FontFamily = family;
        }
    }

    private void IndentationSize_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        var indentationSizeValue = Program.OptionsObject.Editor_IndentationSize = (int)Math.Round(IndentationSize.Value);
        var editors = Program.MainWindow.EditorReferences;
        foreach (var editor in editors)
        {
            editor.editor.Options.IndentationSize = indentationSizeValue;
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
    }

    private void LanguageBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        var originalSource = (ComboBox)e.OriginalSource;
        var selectedItem = (ComboboxItem)originalSource.SelectedItem;
        var lang = Program.Translations.AvailableLanguageIDs.FirstOrDefault(x => x == selectedItem.Value);
        try
        {
            Program.Translations.LoadLanguage(lang);
            Program.OptionsObject.Language = lang;
            Program.MainWindow.Language_Translate();
            Program.MainWindow.EvaluateRTL();
            Language_Translate();
            EvaluateRTL();
        }
        catch (Exception ex)
        {
            this.ShowMessageAsync("Error while switching language", $"Details: {ex.Message}", settings: Program.MainWindow.MetroDialogOptions);
        }

    }

    private void ReloadLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        var selectedItem = (ComboboxItem)LanguageBox.SelectedItem;
        var lang = Program.Translations.AvailableLanguageIDs.FirstOrDefault(x => x == selectedItem.Value);
        try
        {
            Program.Translations.LoadLanguage(lang);
            Program.OptionsObject.Language = lang;
            Program.MainWindow.Language_Translate();
            Language_Translate();
        }
        catch (Exception ex)
        {
            this.ShowMessageAsync("Error while reloading language", $"Details: {ex.Message}", settings: Program.MainWindow.MetroDialogOptions);
        }
    }

    private void ActionOnCloseBox_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Program.OptionsObject.ActionOnClose = (ActionOnClose)ActionOnCloseBox.SelectedIndex;
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

    private void UseBlendEffect_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Program.OptionsObject.Editor_UseBlendEffect = UseBlendEffect.IsChecked.Value;
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
        if (val && !Program.DiscordClient.IsInitialized)
        {
            DiscordPresenceTime.IsEnabled = true;
            DiscordPresenceFile.IsEnabled = true;

            Program.DiscordClient = new DiscordRpcClient(Constants.DiscordRPCAppID);
            Program.DiscordTime = Timestamps.Now;

            // Init Discord RPC
            Program.DiscordClient.Initialize();

            Program.MainWindow.UpdateWindowTitle();
        }
        else if (!val && Program.DiscordClient.IsInitialized)
        {
            DiscordPresenceTime.IsEnabled = false;
            DiscordPresenceFile.IsEnabled = false;
            Program.DiscordClient.Dispose();
        }
    }

    private void DiscordPresenceTime_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Debug.Assert(DiscordPresenceTime.IsChecked != null, "DiscordPresenceTime.IsChecked != null");
        var val = DiscordPresenceTime.IsChecked.Value;
        Program.OptionsObject.Program_DiscordPresenceTime = val;
        Program.DiscordClient.SetPresence(new RichPresence
        {
            Timestamps = val ? Program.DiscordTime : null,
            State = Program.OptionsObject.Program_DiscordPresenceFile ? "Idle" : null,
            Assets = new Assets
            {
                LargeImageKey = "immagine"
            },
            Buttons = new Button[]
            {
                new Button() { Label = Constants.GetSPCodeText, Url = Constants.GitHubLatestRelease }
            }
        });
        // Calling this to set State to the opened file
        Program.MainWindow.UpdateWindowTitle();
    }

    private void DiscordPresenceFile_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Debug.Assert(DiscordPresenceFile.IsChecked != null, "DiscordPresenceFile.IsChecked != null");
        var val = DiscordPresenceFile.IsChecked.Value;
        Program.OptionsObject.Program_DiscordPresenceFile = val;
        Program.DiscordClient.SetPresence(new RichPresence
        {
            Timestamps = Program.OptionsObject.Program_DiscordPresenceTime ? Program.DiscordTime : null,
            State = val ? "" : null,
            Assets = new Assets
            {
                LargeImageKey = "immagine"
            },
            Buttons = new Button[]
            {
                new Button() { Label = Constants.GetSPCodeText, Url = Constants.GitHubLatestRelease }
            }
        });
        // Calling this to set State to the opened file
        Program.MainWindow.UpdateWindowTitle();
    }

    private void AutoSave_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        var newIndex = AutoSave.SelectedIndex;
        var editors = Program.MainWindow.EditorReferences;
        if (newIndex == 0)
        {
            Program.OptionsObject.Editor_AutoSave = false;
            foreach (var editor in editors)
            {
                if (editor.AutoSaveTimer.Enabled)
                {
                    editor.AutoSaveTimer.Stop();
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

            foreach (var editor in editors)
            {
                editor.StartAutoSaveTimer();
            }
        }
    }

    private void TabToAutocomplete_Changed(object sender, RoutedEventArgs e)
    {
        if (!AllowChanging)
        {
            return;
        }

        Debug.Assert(ShowTabs.IsChecked != null, "TabToAutocomplete.IsChecked != null");
        Program.OptionsObject.Editor_TabToAutocomplete = UseTabToAutocomplete.IsChecked.Value;
    }

    #endregion

    #region Methods
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

        for (var i = 0; i < Program.Translations.AvailableLanguages.Count; ++i)
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
        UseTabToAutocomplete.IsChecked = Program.OptionsObject.Editor_TabToAutocomplete;
        FontFamilyTB.Text = Translate("FontFamily");
        FontFamilyCB.SelectedValue = new FontFamily(Program.OptionsObject.Editor_FontFamily);
        IndentationSize.Value = Program.OptionsObject.Editor_IndentationSize;
        HardwareSalts.IsChecked = Program.OptionsObject.Program_UseHardwareSalts;
        UseBlendEffect.IsChecked = Program.OptionsObject.Editor_UseBlendEffect;
        DiscordPresence.IsChecked = Program.OptionsObject.Program_DiscordPresence;
        DiscordPresenceTime.IsChecked = Program.OptionsObject.Program_DiscordPresenceTime;
        DiscordPresenceFile.IsChecked = Program.OptionsObject.Program_DiscordPresenceFile;
        DiscordPresenceTime.IsEnabled = Program.OptionsObject.Program_DiscordPresence;
        DiscordPresenceFile.IsEnabled = Program.OptionsObject.Program_DiscordPresence;
    }

    private void SetupActionOnCloseBox()
    {
        AllowChanging = false;
        var ActionOnCloseMessages = new Dictionary<ActionOnClose, string>
        {
            { ActionOnClose.Prompt, Translate("ActionClosePrompt") },
            { ActionOnClose.Save, Translate("Save") },
            { ActionOnClose.DontSave, Translate("DontSave") }
        };

        ActionOnCloseBox.ItemsSource = ActionOnCloseMessages.Values;
        ActionOnCloseBox.SelectedIndex = (int)Program.OptionsObject.ActionOnClose;
        AllowChanging = true;
    }

    private void Language_Translate()
    {
        Title = Translate("Options");
        HardwareSalts.Content = Translate("HardwareEncryption");
        UseBlendEffect.Content = Translate("UseBlendEffect");
        ProgramHeader.Header = Translate("Program");
        HardwareAcc.Content = Translate("HardwareAcc");
        UIAnimation.Content = Translate("UIAnim");
        OpenIncludes.Content = Translate("OpenInc");
        OpenIncludesRecursive.Content = Translate("OpenIncRec");
        AutoUpdate.Content = Translate("AutoUpdate");
        ShowToolBar.Content = Translate("ShowToolbar");
        DynamicISAC.Content = Translate("DynamicISAC");
        DarkTheme.Content = Translate("DarkTheme");
        ThemeColorLabel.Content = Translate("ThemeColor");
        LanguageLabel.Content = Translate("LanguageStr");
        EditorHeader.Header = Translate("Editor");
        FontSizeBlock.Text = Translate("FontSize");
        ScrollSpeedBlock.Text = Translate("ScrollSpeed");
        WordWrap.Content = Translate("WordWrap");
        AgressiveIndentation.Content = Translate("AggIndentation");
        LineReformatting.Content = Translate("ReformatAfterSem");
        TabToSpace.Content = Translate("TabsToSpace");
        AutoCloseBrackets.Content = Translate("AutoCloseBrack");
        AutoCloseStringChars.Content = Translate("AutoCloseStrChr");
        ShowSpaces.Content = Translate("ShowSpaces");
        ShowTabs.Content = Translate("ShowTabs");
        IndentationSizeBlock.Text = Translate("IndentationSize");
        HighlightDeprecateds.Content = Translate("HighDeprecat");
        AutoSaveBlock.Text = Translate("AutoSaveMin");
        DefaultButton.Content = Translate("DefaultValues");
        DiscordPresence.Content = Translate("EnableRPC");
        DiscordPresenceTime.Content = Translate("EnableRPCTime");
        DiscordPresenceFile.Content = Translate("EnableRPCFile");
        DefaultButton.Content = Translate("DefaultValues");
        BackupButton.Content = Translate("BackupOptions");
        LoadButton.Content = Translate("LoadOptions");
        ResetButton.Content = Translate("ResetOptions");
        ActionOnCloseLabel.Content = Translate("ActionOnClose");
        UseTabToAutocomplete.Content = Translate("TabToAutocomplete");
        HotkeysHeader.Header = Translate("Hotkeys");

        SetupActionOnCloseBox();

        foreach (var item in HotkeysGrid.Children)
        {
            if (item is TextBlock tbx && !string.IsNullOrEmpty(tbx.Name))
            {
                tbx.Text = Translate(tbx.Name.Substring(3));
            }
        }
    }

    private void EvaluateRTL()
    {
        FlowDirection = Program.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        FontSizeD.FlowDirection = FlowDirection.LeftToRight;
        IndentationSize.FlowDirection = FlowDirection.LeftToRight;
        ScrollSpeed.FlowDirection = FlowDirection.LeftToRight;
        foreach (var child in RGBSliders1.Children)
        {
            if (child is ColorChangeControl control)
            {
                control.FlowDirection = FlowDirection.LeftToRight;
            }
        }
        foreach (var child in RGBSliders2.Children)
        {
            if (child is ColorChangeControl control)
            {
                control.FlowDirection = FlowDirection.LeftToRight;
            }
        }
    }
    #endregion
}