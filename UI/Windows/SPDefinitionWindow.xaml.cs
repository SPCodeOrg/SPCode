using System;
using MahApps.Metro.Controls;
using SourcepawnCondenser.SourcemodDefinition;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro;

namespace Spcode.UI.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class SPDefinitionWindow : MetroWindow
    {
        SPDefEntry[] defArray;
        ListViewItem[] items;
        Timer searchTimer = new Timer(1000.0);
        readonly Brush errorSearchBoxBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xA0, 0x30, 0));

        public SPDefinitionWindow()
        {
            InitializeComponent();
			Language_Translate();
			if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
			{ ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor), ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme)); }
			errorSearchBoxBrush.Freeze();
			SMDefinition def = Program.Configs[Program.SelectedConfig].GetSMDef();
            if (def == null)
            {
                MessageBox.Show(Program.Translations.GetLanguage("ConfigWrongPars"), Program.Translations.GetLanguage("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }
            List<SPDefEntry> defList = def.Functions.Cast<SPDefEntry>().ToList();
            defList.AddRange(def.Constants.Cast<SPDefEntry>());
            defList.AddRange(def.Enums.Cast<SPDefEntry>());
            defList.AddRange(def.Defines.Cast<SPDefEntry>());
            defList.AddRange(def.Structs.Cast<SPDefEntry>());
            defList.AddRange(def.Methodmaps.Cast<SPDefEntry>());
            defList.AddRange(def.Typedefs.Cast<SPDefEntry>());
            defList.AddRange(def.EnumStructs.Cast<SPDefEntry>());
            foreach (var mm in def.Methodmaps)
            {
	            defList.AddRange(mm.Methods.Cast<SPDefEntry>());
	            defList.AddRange(mm.Fields.Cast<SPDefEntry>());
            }
			foreach (var sm in def.EnumStructs)
			{
				defList.AddRange(sm.Methods.Cast<SPDefEntry>());
				defList.AddRange(sm.Fields.Cast<SPDefEntry>());
			}
            foreach (var e in defList.Where(e => string.IsNullOrWhiteSpace(e.Name)))
			{
				e.Name = $"--{Program.Translations.GetLanguage("NoName")}--";
			}
			defList.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            defArray = defList.ToArray();
            int defArrayLength = defArray.Length;
            items = new ListViewItem[defArrayLength];
            for (int i = 0; i < defArrayLength; ++i)
            {
                items[i] = new ListViewItem() { Content = defArray[i].Name, Tag = defArray[i].Entry };
                SPBox.Items.Add(items[i]);
            }
            searchTimer.Elapsed += searchTimer_Elapsed;
        }

        void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DoSearch();
			searchTimer.Stop();
        }

        private void SPFunctionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object obj = SPBox.SelectedItem;
            if (obj == null) { return; }
            ListViewItem item = (ListViewItem)obj;
            object TagValue = item.Tag;
            if (TagValue != null)
            {
	            if (TagValue is SMFunction sm1)
                {
	                SPNameBlock.Text = sm1.Name;
                    SPFullNameBlock.Text = sm1.FullName;
					SPFileBlock.Text = sm1.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm1.Index, sm1.Length)})";
					SPTypeBlock.Text = "Function";
					SPCommentBox.Text = sm1.CommentString;
                    return;
				}

	            if (TagValue is SMConstant sm2)
	            {
		            SPNameBlock.Text = sm2.Name;
		            SPFullNameBlock.Text = string.Empty;
		            SPFileBlock.Text = sm2.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm2.Index, sm2.Length)})";
		            SPTypeBlock.Text = "Constant";
		            SPCommentBox.Text = string.Empty;
		            return;
	            }
	            
	            if (TagValue is SMEnum sm3)
	            {
		            SPNameBlock.Text = sm3.Name;
		            SPFullNameBlock.Text = string.Empty;
		            SPFileBlock.Text = sm3.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm3.Index, sm3.Length)})";
		            SPTypeBlock.Text = "Enum " + sm3.Entries.Length.ToString() + " entries";
		            StringBuilder outString = new StringBuilder();
		            for (int i = 0; i < sm3.Entries.Length; ++i)
		            {
			            outString.Append((i.ToString() + ".").PadRight(5, ' '));
			            outString.AppendLine(sm3.Entries[i]);
		            }
		            SPCommentBox.Text = outString.ToString();
		            return;
	            }
	            
	            if (TagValue is SMStruct sm4)
	            {
		            SPNameBlock.Text = sm4.Name;
		            SPFullNameBlock.Text = string.Empty;
		            SPFileBlock.Text = sm4.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm4.Index, sm4.Length)})";
		            SPTypeBlock.Text = "Struct";
		            SPCommentBox.Text = string.Empty;
		            return;
	            }
	            
	            if (TagValue is SMDefine sm5)
	            {
		            SPNameBlock.Text = sm5.Name;
		            SPFullNameBlock.Text = string.Empty;
		            SPFileBlock.Text = sm5.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm5.Index, sm5.Length)})";
		            SPTypeBlock.Text = "Definition";
		            SPCommentBox.Text = string.Empty;
		            return;
	            }
	            
	            if (TagValue is SMMethodmap sm6)
	            {
		            SPNameBlock.Text = sm6.Name;
		            SPFullNameBlock.Text = $"{Program.Translations.GetLanguage("TypeStr")}: " + sm6.Type + $" - {Program.Translations.GetLanguage("InheritedFrom")}: {sm6.InheritedType}";
		            SPFileBlock.Text = sm6.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm6.Index, sm6.Length)})";
		            SPTypeBlock.Text = "Methodmap " + sm6.Methods.Count.ToString() + " methods - " + sm6.Fields.Count.ToString() + " fields";
		            StringBuilder outString = new StringBuilder();
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
		            SPFileBlock.Text = sm7.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm7.Index, sm7.Length)})";
		            SPTypeBlock.Text = $"{Program.Translations.GetLanguage("MethodFrom")} {sm7.MethodmapName}";
		            SPCommentBox.Text = sm7.CommentString;
		            return;
	            }

	            if (TagValue is SMMethodmapField sm8)
	            {
		            SPNameBlock.Text = sm8.Name;
		            SPFullNameBlock.Text = sm8.FullName;
		            SPFileBlock.Text = sm8.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm8.Index, sm8.Length)})";
		            SPTypeBlock.Text = $"{Program.Translations.GetLanguage("PropertyFrom")} {sm8.MethodmapName}";
		            SPCommentBox.Text = string.Empty;
		            return;
	            }
	            
	            if (TagValue is SMTypedef sm)
	            {
		            SPNameBlock.Text = sm.Name;
		            SPFullNameBlock.Text = string.Empty;
		            SPFileBlock.Text = sm.File + ".inc" + $" ({string.Format(Program.Translations.GetLanguage("PosLen"), sm.Index, sm.Length)})";
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
                    int itemCount = defArray.Length;
                    string searchString = SPSearchBox.Text.ToLowerInvariant();
                    bool foundOccurence = false;
                    SPBox.Items.Clear();
                    for (int i = 0; i < itemCount; ++i)
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

		private class SPDefEntry
        {
            public string Name;
            public object Entry;

			public static explicit operator SPDefEntry(SMFunction func)
			{
				return new SPDefEntry() { Name = func.Name, Entry = func };
			}
			public static explicit operator SPDefEntry(SMConstant sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMDefine sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMEnum sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMStruct sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMMethodmap sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMMethodmapMethod sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMMethodmapField sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}
			public static explicit operator SPDefEntry(SMTypedef sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm };
			}

			public static explicit operator SPDefEntry(SMEnumStruct sm)
			{
				return new SPDefEntry() { Name = sm.Name, Entry = sm};
			}
		}
    }
}
