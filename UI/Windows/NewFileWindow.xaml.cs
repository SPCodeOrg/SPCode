using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using MahApps.Metro;
using Microsoft.Win32;
using SPCode.UI.Components;
using SPCode.Utils;

namespace SPCode.UI.Windows
{
    public partial class NewFileWindow
    {
        #region Variables
        private readonly string PathStr = Program.Configs[Program.SelectedConfig].SMDirectories[0];
        private bool TemplateEditMode = false;
        private bool TemplateNewMode = false;
        private ICommand textBoxButtonFileCmd;
        private List<ListBoxItem> LBIList;

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
                                    Title = Program.Translations.GetLanguage("New")
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
        #endregion

        #region Constructors
        public NewFileWindow()
        {
            // We cache templates first before calling InitializeComponent, because
            // inside CacheTemplates I set the Program.SelectedTemplatePath variable
            // so the EditorElement has something to work with before initializing

            CacheTemplates();
            InitializeComponent();
            ParseTemplateFile();
            Language_Translate();
            Program.MainWindow.EditorsReferences.Add(PreviewBox);
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            TemplateListBox.SelectedIndex = 0;
            PreviewBox.editor.IsReadOnly = true;
            PreviewBox.editor.FontSize = 12;
            PreviewBox.CompileBox.Visibility = Visibility.Collapsed;
            PreviewBox.StatusLine_Column.Visibility = Visibility.Collapsed;
            PreviewBox.StatusLine_FontSize.Visibility = Visibility.Collapsed;
            PreviewBox.StatusLine_Offset.Visibility = Visibility.Collapsed;
            PreviewBox.StatusLine_Line.Visibility = Visibility.Collapsed;
            PreviewBox.StatusLine_SelectionLength.Visibility = Visibility.Collapsed;

        }
        #endregion

        #region Events
        private void TemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateListBox.SelectedItem != null)
            {
                var templateInfo = (TemplateListBox.SelectedItem as ListBoxItem).Tag as TemplateInfo;
                Program.SelectedTemplatePath = templateInfo.Path;
                PreviewBox.editor.Text = File.ReadAllText(templateInfo.Path);
                PathBox.Text = Path.Combine(PathStr, templateInfo.NewName);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateEditMode)
            {
                var tempName = TbxRenameTemplate.Text;
                if (!IsValidTemplate(ref tempName, out var message))
                {
                    TbxRenameTemplate.BorderBrush = new SolidColorBrush(Colors.Red);
                    LblError.Visibility = Visibility.Visible;
                    LblError.Content = message;
                    return;
                }

                TemplateEditMode = false;

                foreach (ListBoxItem item in TemplateListBox.Items)
                {
                    item.IsEnabled = true;
                }

                PathBox.IsEnabled = true;
                PreviewBox.editor.IsReadOnly = true;
                SaveTemplate(tempName);
                HideVisuals();
                return;
            }

            if (TemplateNewMode)
            {
                TemplateNewMode = false;

                foreach (ListBoxItem item in TemplateListBox.Items)
                {
                    item.IsEnabled = true;
                }

                PathBox.IsEnabled = true;
                PreviewBox.editor.IsReadOnly = true;
                CreateTemplate();
                HideVisuals();
                return;
            }
            GoToSelectedTemplate();
        }

        private void TemplateListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                GoToSelectedTemplate();
            }
        }

        private void TemplateListItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GoToSelectedTemplate();
            }
        }

        private void TemplateListItem_Context_Add(object sender, RoutedEventArgs e)
        {
            TemplateNewMode = true;

            foreach (ListBoxItem item in TemplateListBox.Items)
            {
                item.IsEnabled = false;
            }

            PathBox.IsEnabled = false;
            PreviewBox.editor.IsReadOnly = false;
            PreviewBox.editor.TextArea.Focus();
            PreviewBox.editor.Clear();

            ShowVisuals(true);
        }

        private void TemplateListItem_Context_Edit(object sender, RoutedEventArgs e)
        {
            TemplateEditMode = true;

            foreach (ListBoxItem item in TemplateListBox.Items)
            {
                item.IsEnabled = false;
            }

            PathBox.IsEnabled = false;
            PreviewBox.editor.IsReadOnly = false;
            PreviewBox.editor.TextArea.Focus();

            ShowVisuals(false);
        }

        private void TemplateListItem_Context_Delete(object sender, RoutedEventArgs e)
        {
            DeleteTemplate(TemplateListBox.SelectedItem as ListBoxItem);
        }

        private void NewFileWind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void NewFileWind_Closing(object sender, CancelEventArgs e)
        {
            Program.MainWindow.EditorsReferences.Remove(PreviewBox);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Cache all templates into a local List of ListBoxItems
        /// </summary>
        private void CacheTemplates()
        {
            if (File.Exists(Paths.GetTemplatesFilePath()))
            {
                LBIList = new List<ListBoxItem>();
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

                            var lbi = new ListBoxItem()
                            {
                                Tag = new TemplateInfo()
                                {
                                    Name = NameStr,
                                    FileName = FileNameStr,
                                    NewName = NewNameStr,
                                    Path = FilePathStr
                                },
                                Content = NameStr
                            };

                            lbi.MouseDoubleClick += TemplateListItem_MouseDoubleClick;
                            lbi.KeyDown += TemplateListItem_KeyDown;
                            LBIList.Add(lbi);
                        }
                    }
                }
                Program.SelectedTemplatePath = LBIList.Count > 0 ? (LBIList[0].Tag as TemplateInfo).Path : null;
            }
        }

        private void ParseTemplateFile()
        {
            foreach (var item in LBIList)
            {
                TemplateListBox.Items.Add(item);
            }
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

        private void GoToSelectedTemplate()
        {
            var destFile = new FileInfo(PathBox.Text);
            var templateInfo = (TemplateListBox.SelectedItem as ListBoxItem).Tag as TemplateInfo;
            File.Copy(templateInfo.Path, destFile.FullName, true);
            Program.MainWindow.TryLoadSourceFile(destFile.FullName, true, true, true);

            Close();
        }

        private void SaveTemplate(string name)
        {
            var temp = TemplateListBox.SelectedItem as ListBoxItem;
            var tempFilePath = Paths.GetTemplatesFilePath();

            // Save the file's content
            var path = (temp.Tag as TemplateInfo).Path;
            File.WriteAllText(path, PreviewBox.editor.Text);

            // Save the new entry in Templates.xml
            using Stream stream = File.OpenRead(tempFilePath);
            var doc = new XmlDocument();
            doc.Load(stream);
            stream.Dispose();

            foreach (XmlElement elem in doc.ChildNodes[0].ChildNodes)
            {
                if (elem.Attributes["Name"].Value == temp.Content.ToString())
                {
                    elem.Attributes["Name"].Value = name;
                    doc.Save(tempFilePath);
                    break;
                }
            }

            // Save new list item
            temp.Content = name;
        }

        private void DeleteTemplate(ListBoxItem temp)
        {
            TemplateListBox.Items.RemoveAt(TemplateListBox.SelectedIndex);
            File.Delete(new FileInfo((temp.Tag as TemplateInfo).Path).FullName);
            using Stream stream = File.OpenRead(Paths.GetTemplatesFilePath());
            var doc = new XmlDocument();
            doc.Load(stream);
            stream.Dispose();
            foreach (XmlNode node in doc.ChildNodes[0].ChildNodes)
            {
                if (node.Attributes["Name"].Value == temp.Content.ToString())
                {
                    doc.ChildNodes[0].RemoveChild(node);
                    doc.Save(Paths.GetTemplatesFilePath());
                    return;
                }
            }
        }

        private void CreateTemplate()
        {
            var tempFilePath = Paths.GetTemplatesFilePath();

            using Stream stream = File.OpenRead(tempFilePath);
            var doc = new XmlDocument();
            doc.Load(stream);
            stream.Dispose();

            var newNode = doc.CreateElement("Template");
            newNode.SetAttribute("Name", TbxRenameTemplate.Text);
            newNode.SetAttribute("NewName", PathBox.Text);
            newNode.SetAttribute("File", new FileInfo(PathBox.Text).Name);

            doc.ChildNodes[0].AppendChild(newNode);
            doc.Save(tempFilePath);
        }

        private void ShowVisuals(bool isNewTemplate)
        {
            PreviewBlock.Text = $"{Program.Translations.GetLanguage("Name")}:";
            TbxRenameTemplate.Text = isNewTemplate ? "" : (TemplateListBox.SelectedItem as ListBoxItem).Content.ToString();
            TbxRenameTemplate.Visibility = Visibility.Visible;
            var mg = PreviewBox.Margin;
            mg.Top += 10;
            PreviewBox.Margin = mg;
        }

        private void HideVisuals()
        {
            PreviewBlock.Text = $"{Program.Translations.GetLanguage("Preview")}:";
            TbxRenameTemplate.Visibility = Visibility.Collapsed;
            LblError.Visibility = Visibility.Collapsed;
            var mg = PreviewBox.Margin;
            mg.Top -= 10;
            PreviewBox.Margin = mg;
        }

        private bool IsValidTemplate(ref string name, out string message)
        {
            message = string.Empty;
            var tempName = name;

            if (TemplateListBox.Items.Cast<ListBoxItem>().ToList().Any(x => x.Content.ToString().ToLower() == tempName.ToLower()))
            {
                message = Program.Translations.GetLanguage("TemplateExists");
                return false;
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) 
            {
                message = Program.Translations.GetLanguage("EmptyName");
                return false;
            }

            var arr = name.ToCharArray();
            arr = Array.FindAll(arr, c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-');

            if (arr.Length == 0)
            {
                message = Program.Translations.GetLanguage("IllegalCharacters");
                return false;
            }

            name = new string(arr);
            return true;
        }
        #endregion
    }
}