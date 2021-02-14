using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro;
using MahApps.Metro.Controls;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.UI.Windows
{
    /// <summary>
    ///     Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class SPDefinitionWindow
    {
        private readonly SMBaseDefinition[] defArray;
        private readonly Brush errorSearchBoxBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xA0, 0x30, 0));
        private readonly ListViewItem[] items;
        private readonly Timer searchTimer = new Timer(1000.0);

        public SPDefinitionWindow()
        {
            InitializeComponent();
            Language_Translate();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            errorSearchBoxBrush.Freeze();
            var def = Program.Configs[Program.SelectedConfig].GetSMDef();
            if (def == null)
            {
                MessageBox.Show(Program.Translations.GetLanguage("ConfigWrongPars"),
                    Program.Translations.GetLanguage("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            var defList = new List<SMBaseDefinition>();
            defList.AddRange(def.Functions);
            defList.AddRange(def.Constants);
            defList.AddRange(def.Enums);
            defList.AddRange(def.Defines);
            defList.AddRange(def.Structs);
            defList.AddRange(def.Methodmaps);
            defList.AddRange(def.Typedefs);
            defList.AddRange(def.EnumStructs);
            defList.AddRange(def.Variables);
            foreach (var mm in def.Methodmaps)
            {
                defList.AddRange(mm.Methods);
                defList.AddRange(mm.Fields);
            }

            foreach (var es in def.EnumStructs)
            {
                defList.AddRange(es.Methods);
                defList.AddRange(es.Fields);
            }

            foreach (var e in defList.Where(e => string.IsNullOrWhiteSpace(e.Name)))
            {
                e.Name = $"--{Program.Translations.GetLanguage("NoName")}--";
            }

            defList.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            defArray = defList.ToArray();
            var defArrayLength = defArray.Length;
            items = new ListViewItem[defArrayLength];
            for (var i = 0; i < defArrayLength; ++i)
            {
                items[i] = new ListViewItem { Content = defArray[i].Name, Tag = defArray[i] };
                SPBox.Items.Add(items[i]);
            }

            searchTimer.Elapsed += SearchTimer_Elapsed;
        }

        private void SearchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DoSearch();
            searchTimer.Stop();
        }

        private void SPFunctionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = SPBox.SelectedItem;
            if (obj == null)
            {
                return;
            }

            var item = (ListViewItem)obj;
            var TagValue = item.Tag;
            if (TagValue != null)
            {
                if (TagValue is SMFunction sm1)
                {
                    SPNameBlock.Text = sm1.Name;
                    SPFullNameBlock.Text = sm1.FullName;
                    SPFileBlock.Text = sm1.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm1.Index, sm1.Length)}";
                    SPTypeBlock.Text = "Function";
                    SPCommentBox.Text = "Comment: " + sm1.CommentString + "\nType: " + sm1.FunctionKind;
                    return;
                }

                if (TagValue is SMConstant sm2)
                {
                    SPNameBlock.Text = sm2.Name;
                    SPFullNameBlock.Text = string.Empty;
                    SPFileBlock.Text = sm2.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm2.Index, sm2.Length)}";
                    SPTypeBlock.Text = "Constant";
                    SPCommentBox.Text = string.Empty;
                    return;
                }

                if (TagValue is SMEnum sm3)
                {
                    SPNameBlock.Text = sm3.Name;
                    SPFullNameBlock.Text = string.Empty;
                    SPFileBlock.Text = sm3.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm3.Index, sm3.Length)}";
                    SPTypeBlock.Text = "Enum " + sm3.Entries.Length + " entries";
                    var outString = new StringBuilder();
                    for (var i = 0; i < sm3.Entries.Length; ++i)
                    {
                        outString.Append((i + ".").PadRight(5, ' '));
                        outString.AppendLine(sm3.Entries[i]);
                    }

                    SPCommentBox.Text = outString.ToString();
                    return;
                }

                if (TagValue is SMStruct sm4)
                {
                    SPNameBlock.Text = sm4.Name;
                    SPFullNameBlock.Text = string.Empty;
                    SPFileBlock.Text = sm4.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm4.Index, sm4.Length)}";
                    SPTypeBlock.Text = "Struct";
                    SPCommentBox.Text = string.Empty;
                    return;
                }

                if (TagValue is SMDefine sm5)
                {
                    SPNameBlock.Text = sm5.Name;
                    SPFullNameBlock.Text = string.Empty;
                    SPFileBlock.Text = sm5.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm5.Index, sm5.Length)}";
                    SPTypeBlock.Text = "Definition";
                    SPCommentBox.Text = string.Empty;
                    return;
                }

                if (TagValue is SMMethodmap sm6)
                {
                    SPNameBlock.Text = sm6.Name;
                    SPFullNameBlock.Text = $"{Program.Translations.GetLanguage("TypeStr")}: " + sm6.Type +
                                           $" - {Program.Translations.GetLanguage("InheritedFrom")}: {sm6.InheritedType}";
                    SPFileBlock.Text = sm6.File + ".inc" +
                                       $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm6.Index, sm6.Length)})";
                    SPTypeBlock.Text = "Methodmap " + sm6.Methods.Count + " methods - " + sm6.Fields.Count + " fields";
                    var outString = new StringBuilder();
                    outString.AppendLine("Methods:");
                    foreach (var m in sm6.Methods)
                    {
                        outString.AppendLine(m.FullName);
                    }

                    outString.AppendLine();
                    outString.AppendLine("Fields:");
                    foreach (var f in sm6.Fields)
                    {
                        outString.AppendLine(f.FullName);
                    }

                    SPCommentBox.Text = outString.ToString();
                    return;
                }

                if (TagValue is SMMethodmapMethod sm7)
                {
                    SPNameBlock.Text = sm7.Name;
                    SPFullNameBlock.Text = sm7.FullName;
                    SPFileBlock.Text = sm7.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm7.Index, sm7.Length)}";
                    SPTypeBlock.Text = $"{Program.Translations.GetLanguage("MethodFrom")} {sm7.MethodmapName}";
                    SPCommentBox.Text = sm7.CommentString;
                    return;
                }

                if (TagValue is SMMethodmapField sm8)
                {
                    SPNameBlock.Text = sm8.Name;
                    SPFullNameBlock.Text = sm8.FullName;
                    SPFileBlock.Text = sm8.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm8.Index, sm8.Length)}";
                    SPTypeBlock.Text = $"{Program.Translations.GetLanguage("PropertyFrom")} {sm8.MethodmapName}";
                    SPCommentBox.Text = string.Empty;
                    return;
                }

                if (TagValue is SMTypedef sm)
                {
                    SPNameBlock.Text = sm.Name;
                    SPFullNameBlock.Text = string.Empty;
                    SPFileBlock.Text = sm.File + ".inc" +
                                       $" {string.Format(Program.Translations.GetLanguage("PosLen"), sm.Index, sm.Length)}";
                    SPTypeBlock.Text = "Typedef/Typeset";
                    SPCommentBox.Text = sm.FullName;
                    return;
                }

                if (TagValue is string value)
                {
                    SPNameBlock.Text = (string)item.Content;
                    SPFullNameBlock.Text = value;
                    SPFileBlock.Text = string.Empty;
                    SPCommentBox.Text = string.Empty;
                    return;
                }
            }

            SPNameBlock.Text = (string)item.Content;
            SPFullNameBlock.Text = string.Empty;
            SPFileBlock.Text = string.Empty;
            SPTypeBlock.Text = string.Empty;
            SPCommentBox.Text = string.Empty;
        }

        private void SPFunctionsListBox_DoubleClick(object sender, RoutedEventArgs e)
        {
            GoToDefinition();
        }

        private void SPFunctionsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GoToDefinition();
            }
        }

        private void GoToDefinition()
        {
            var item = (ListViewItem)SPBox.SelectedItem;
            if (item == null)
            {
                return;
            }

            Close();

            var sm = (SMBaseDefinition)item.Tag;
            var config = Program.Configs[Program.SelectedConfig].SMDirectories;

            foreach (var cfg in config)
            {
                if (Program.MainWindow.TryLoadSourceFile(Path.GetFullPath(Path.Combine(cfg, "include", sm.File)) + ".inc", true, false, true))
                {
                    var ee = Program.MainWindow.GetCurrentEditorElement();
                    Debug.Assert(ee != null);
                    ee.editor.TextArea.Caret.Offset = sm.Index;
                    ee.editor.TextArea.Caret.BringCaretToView();
                    return;
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SPProgress.IsIndeterminate = true;
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void DoSearch()
        {
            Dispatcher?.Invoke(() =>
            {
                var itemCount = defArray.Length;
                var searchString = SPSearchBox.Text.ToLowerInvariant();
                var foundOccurence = false;
                SPBox.Items.Clear();
                for (var i = 0; i < itemCount; ++i)
                {
                    if (defArray[i].Name.ToLowerInvariant().Contains(searchString))
                    {
                        foundOccurence = true;
                        SPBox.Items.Add(items[i]);
                    }
                }

                SPSearchBox.Background = foundOccurence ? Brushes.Transparent : errorSearchBoxBrush;
                SPProgress.IsIndeterminate = false;
            });
        }

        private void Language_Translate()
        {
            TextBoxHelper.SetWatermark(SPSearchBox, Program.Translations.GetLanguage("Search"));
            /*if (Program.Translations.GetLanguage("IsDefault)
            {
                return;
            }*/
        }


    }
}