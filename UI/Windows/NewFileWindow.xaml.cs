using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using MahApps.Metro;
using Microsoft.Win32;

namespace Spedit.UI.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class NewFileWindow
    {
        string PathStr = "sourcepawn\\scripts";
        Dictionary<string, TemplateInfo> TemplateDictionary;
        public NewFileWindow()
        {
            InitializeComponent();
			Language_Translate();
			if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
			{ ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor), ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme)); }
			ParseTemplateFile();
            TemplateListBox.SelectedIndex = 0;
        }

        private void ParseTemplateFile()
        {
            TemplateDictionary = new Dictionary<string, TemplateInfo>();
            if (File.Exists("sourcepawn\\templates\\Templates.xml"))
            {
                using (Stream stream = File.OpenRead("sourcepawn\\templates\\Templates.xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    if (doc.ChildNodes.Count <= 0) return;
                    if (doc.ChildNodes[0].Name != "Templates") return;
                    
                    XmlNode mainNode = doc.ChildNodes[0];
                    for (int i = 0; i < mainNode.ChildNodes.Count; ++i)
                    {
                        if (mainNode.ChildNodes[i].Name == "Template")
                        {
                            XmlAttributeCollection attributes = mainNode.ChildNodes[i].Attributes;
                            string NameStr = attributes?["Name"].Value;
                            string FileNameStr = attributes?["File"].Value;
                            string NewNameStr = attributes?["NewName"].Value;
                            
                            Debug.Assert(FileNameStr != null, nameof(FileNameStr) + " != null");
                            string FilePathStr = Path.Combine("sourcepawn\\templates\\", FileNameStr);
                            if (File.Exists(FilePathStr))
                            {
                                Debug.Assert(NameStr != null, nameof(NameStr) + " != null");
                                TemplateDictionary.Add(NameStr, new TemplateInfo { Name = NameStr, FileName = FileNameStr, Path = FilePathStr, NewName = NewNameStr });
                                TemplateListBox.Items.Add(NameStr);
                            }
                        }
                    }
                }
            }
        }

        private void TemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TemplateInfo templateInfo = TemplateDictionary[(string)TemplateListBox.SelectedItem];
            PrevieBox.Text = File.ReadAllText(templateInfo.Path);
            PathBox.Text = Path.Combine(PathStr, templateInfo.NewName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FileInfo destFile = new FileInfo(PathBox.Text);
            TemplateInfo templateInfo = TemplateDictionary[(string)TemplateListBox.SelectedItem];
            File.Copy(templateInfo.Path, destFile.FullName, true);
            Program.MainWindow.TryLoadSourceFile(destFile.FullName, true, true, true);
            Close();
        }

		private void Language_Translate()
		{
			if (Program.Translations.IsDefault)
			{
				return;
			}
			PreviewBlock.Text = $"{Program.Translations.GetLanguage("Preview")}:";
			SaveButton.Content = Program.Translations.GetLanguage("Save");
		}

        private ICommand textBoxButtonFileCmd;

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
                                var dialog = new SaveFileDialog
                                {
                                    AddExtension = true,
                                    Filter = "Sourcepawn Files (*.sp *.inc)|*.sp;*.inc|All Files (*.*)|*.*",
                                    OverwritePrompt = true,
                                    Title = Program.Translations.GetLanguage("NewFile")
                                };
                                var result = dialog.ShowDialog();

                                Debug.Assert(result != null, nameof(result) + " != null");
                                if (result.Value)
                                {
                                    box.Text = dialog.FileName;
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

    public class TemplateInfo
    {
        public string Name;
        public string FileName;
        public string Path;
        public string NewName;
    }
}
