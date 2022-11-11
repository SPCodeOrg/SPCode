using System.Collections.Generic;
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
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows;

public partial class SPDefinitionWindow
{
    #region Variables
    private readonly SMBaseDefinition[] defArray;
    private readonly Brush errorSearchBoxBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xA0, 0x30, 0));
    private readonly ListViewItem[] items;
    private readonly Timer searchTimer = new(1000.0);
    #endregion

    #region Constructors
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
            MessageBox.Show(Translate("ConfigWrongPars"),
                Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        var defList = new List<SMBaseDefinition>();
        defList.AddRange(def.Functions);
        defList.AddRange(def.ConstVariables);
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
            e.Name = $"--{Translate("NoName")}--";
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
    #endregion

    #region Events
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
            if (TagValue is SMFunction fn)
            {
                SPNameBlock.Text = fn.Name;
                SPFullNameBlock.Text = fn.FullName;
                SPFileBlock.Text = fn.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), fn.Index, fn.Length)}";
                SPTypeBlock.Text = "Function";
                SPCommentBox.Text = "Comment: " + fn.CommentString + "\nType: " + fn.FunctionKind;
                return;
            }

            if (TagValue is SMConstant constant)
            {
                SPNameBlock.Text = constant.Name;
                SPFullNameBlock.Text = string.Empty;
                SPFileBlock.Text = constant.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), constant.Index, constant.Length)}";
                SPTypeBlock.Text = "Constant";
                SPCommentBox.Text = string.Empty;
                return;
            }

            if (TagValue is SMEnum en)
            {
                SPNameBlock.Text = en.Name;
                SPFullNameBlock.Text = string.Empty;
                SPFileBlock.Text = en.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), en.Index, en.Length)}";
                SPTypeBlock.Text = "Enum " + en.Entries.Length + " entries";
                var outString = new StringBuilder();
                for (var i = 0; i < en.Entries.Length; ++i)
                {
                    outString.Append((i + ".").PadRight(5, ' '));
                    outString.AppendLine(en.Entries[i]);
                }

                SPCommentBox.Text = outString.ToString();
                return;
            }

            if (TagValue is SMStruct str)
            {
                SPNameBlock.Text = str.Name;
                SPFullNameBlock.Text = string.Empty;
                SPFileBlock.Text = str.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), str.Index, str.Length)}";
                SPTypeBlock.Text = "Struct";
                SPCommentBox.Text = string.Empty;
                return;
            }

            if (TagValue is SMDefine define)
            {
                SPNameBlock.Text = define.Name;
                SPFullNameBlock.Text = string.Empty;
                SPFileBlock.Text = define.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), define.Index, define.Length)}";
                SPTypeBlock.Text = "Definition";
                SPCommentBox.Text = string.Empty;
                return;
            }

            if (TagValue is SMMethodmap methodmap)
            {
                SPNameBlock.Text = methodmap.Name;
                SPFullNameBlock.Text = $"{Translate("InheritedFrom")}: {methodmap.InheritedType}";
                SPFileBlock.Text = methodmap.File + ".inc" +
                                   $" ({string.Format(Translate("PosLen"), methodmap.Index, methodmap.Length)})";
                SPTypeBlock.Text = "Methodmap " + methodmap.Methods.Count + " methods - " + methodmap.Fields.Count + " fields";
                var outString = new StringBuilder();
                outString.AppendLine("Methods:");
                foreach (var m in methodmap.Methods)
                {
                    outString.AppendLine(m.FullName);
                }

                outString.AppendLine();
                outString.AppendLine("Fields:");
                foreach (var f in methodmap.Fields)
                {
                    outString.AppendLine(f.FullName);
                }

                SPCommentBox.Text = outString.ToString();
                return;
            }

            if (TagValue is SMObjectMethod method)
            {
                SPNameBlock.Text = method.Name;
                SPFullNameBlock.Text = method.FullName;
                SPFileBlock.Text = method.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), method.Index, method.Length)}";
                SPTypeBlock.Text = $"{Translate("MethodFrom")} {method.ClassName}";
                SPCommentBox.Text = method.CommentString;
                return;
            }

            if (TagValue is SMObjectField field)
            {
                SPNameBlock.Text = field.Name;
                SPFullNameBlock.Text = field.FullName;
                SPFileBlock.Text = field.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), field.Index, field.Length)}";
                SPTypeBlock.Text = $"{Translate("PropertyFrom")} {field.ClassName}";
                SPCommentBox.Text = string.Empty;
                return;
            }

            if (TagValue is SMTypedef typedef)
            {
                SPNameBlock.Text = typedef.Name;
                SPFullNameBlock.Text = string.Empty;
                SPFileBlock.Text = typedef.File + ".inc" +
                                   $" {string.Format(Translate("PosLen"), typedef.Index, typedef.Length)}";
                SPTypeBlock.Text = "Typedef/Typeset";
                SPCommentBox.Text = typedef.FullName;
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

    private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SPProgress.IsIndeterminate = true;
        searchTimer.Stop();
        searchTimer.Start();
    }
    #endregion

    #region Methods
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
            if (Program.MainWindow.TryLoadSourceFile(Path.GetFullPath(Path.Combine(cfg, "include", sm.File)) + ".inc", out var ee, false, true) && ee != null)
            {
                ee.editor.TextArea.Caret.Offset = sm.Index;
                ee.editor.TextArea.Caret.BringCaretToView();
                return;
            }
        }
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
        Title = Translate("ParsedDefWindow");
        TextBoxHelper.SetWatermark(SPSearchBox, Translate("Search"));
    }
    #endregion
}