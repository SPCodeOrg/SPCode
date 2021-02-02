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
using SPCode.Utils;

namespace SPCode.UI.Windows
{
    /// <summary>
    ///     Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class NewFileWindow
    {
        // From https://github.com/CrucialCoding/Spedit/commit/d2edde54385bb76eccdf13934ce33a8a7e3cc31e
        private readonly string PathStr = Program.Configs[Program.SelectedConfig].SMDirectories[0];
        private Dictionary<string, TemplateInfo> TemplateDictionary;

        private ICommand textBoxButtonFileCmd;

        public NewFileWindow()
        {
            InitializeComponent();
            Language_Translate();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            ParseTemplateFile();
            TemplateListBox.SelectedIndex = 0;
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

        private void ParseTemplateFile()
        {
            TemplateDictionary = new Dictionary<string, TemplateInfo>();
            if (File.Exists(Paths.GetTemplatesFilePath()))
            {
                using Stream stream = File.OpenRead(Paths.GetTemplatesFilePath());
                var doc = new XmlDocument();
                doc.Load(stream);
                if (doc.ChildNodes.Count <= 0)
                {
                    return;
                }

                if (doc.ChildNodes[0].Name != "Templates")
                {
                    return;
                }

                var mainNode = doc.ChildNodes[0];
                for (var i = 0; i < mainNode.ChildNodes.Count; ++i)
                {
                    if (mainNode.ChildNodes[i].Name == "Template")
                    {
                        var attributes = mainNode.ChildNodes[i].Attributes;
                        var NameStr = attributes?["Name"].Value;
                        var FileNameStr = attributes?["File"].Value;
                        var NewNameStr = attributes?["NewName"].Value;

                        Debug.Assert(FileNameStr != null, nameof(FileNameStr) + " != null");
                        var FilePathStr = Path.Combine(Paths.GetTemplatesDirectory(), FileNameStr);
                        if (File.Exists(FilePathStr))
                        {
                            Debug.Assert(NameStr != null, nameof(NameStr) + " != null");
                            TemplateDictionary.Add(NameStr,
                                new TemplateInfo
                                {
                                    Name = NameStr,
                                    FileName = FileNameStr,
                                    Path = FilePathStr,
                                    NewName = NewNameStr
                                });
                            TemplateListBox.Items.Add(NameStr);
                        }
                    }
                }
            }
        }

        private void TemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var templateInfo = TemplateDictionary[(string)TemplateListBox.SelectedItem];
            PrevieBox.Text = File.ReadAllText(templateInfo.Path);
            PathBox.Text = Path.Combine(PathStr, templateInfo.NewName);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GoToSelectedTemplate();
        }

        private void TemplateListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GoToSelectedTemplate();
        }

        private void TemplateListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GoToSelectedTemplate();
            }
        }

        private void GoToSelectedTemplate()
        {
            var destFile = new FileInfo(PathBox.Text);
            var templateInfo = TemplateDictionary[(string)TemplateListBox.SelectedItem];
            File.Copy(templateInfo.Path, destFile.FullName, true);
            Program.MainWindow.TryLoadSourceFile(destFile.FullName, true, true, true);
            Close();
        }
    }

    public class TemplateInfo
    {
        public string FileName;
        public string Name;
        public string NewName;
        public string Path;
    }
}