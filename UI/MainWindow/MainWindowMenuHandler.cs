using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using SPCode.Interop;
using SPCode.Interop.Updater;
using SPCode.UI.Windows;
using SPCode.Utils;
using SPCode.Utils.Models;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        #region Events
        private void FileMenu_Open(object sender, RoutedEventArgs e)
        {
            var editors = GetAllEditorElements();
            var EditorsAreOpen = false;
            if (editors != null)
            {
                EditorsAreOpen = editors.Length > 0;
            }

            var EditorIsSelected = GetCurrentEditorElement() != null;
            MenuI_Save.IsEnabled = EditorIsSelected;
            MenuI_SaveAs.IsEnabled = EditorIsSelected;
            MenuI_Close.IsEnabled = EditorIsSelected;
            MenuI_SaveAll.IsEnabled = EditorsAreOpen;
            MenuI_CloseAll.IsEnabled = EditorsAreOpen;
        }

        private void Menu_ClearRecent(object sender, RoutedEventArgs e)
        {
            Program.OptionsObject.RecentFiles.Clear();
            MenuI_Recent.Items.Clear();
            MenuI_Recent.IsEnabled = false;
        }

        private void Menu_ReopenLastClosedTab(object sender, RoutedEventArgs e)
        {
            Command_ReopenLastClosedTab();
        }

        private void Menu_New(object sender, RoutedEventArgs e)
        {
            Command_New();
        }

        private void Menu_NewFromTemplate(object sender, RoutedEventArgs e)
        {
            Command_NewFromTemplate();
        }

        private void Menu_Open(object sender, RoutedEventArgs e)
        {
            Command_Open();
        }

        private void Menu_Save(object sender, RoutedEventArgs e)
        {
            Command_Save();
        }

        private void Menu_SaveAll(object sender, RoutedEventArgs e)
        {
            Command_SaveAll();
        }

        private void Menu_SaveAs(object sender, RoutedEventArgs e)
        {
            Command_SaveAs();
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            Command_Close();
        }

        private void Menu_CloseAll(object sender, RoutedEventArgs e)
        {
            Command_CloseAll();
        }

        private void EditMenu_Open(object sender, RoutedEventArgs e)
        {
            var ee = GetCurrentEditorElement();
            var menu = (MenuItem)sender;
            if (ee == null)
            {
                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        menuItem.IsEnabled = false;
                    }
                }
            }
            else
            {
                MenuI_Undo.IsEnabled = ee.editor.CanUndo;
                MenuI_Redo.IsEnabled = ee.editor.CanRedo;
                for (var i = 2; i < menu.Items.Count; ++i)
                {
                    if (menu.Items[i] is MenuItem item)
                    {
                        item.IsEnabled = true;
                    }
                }
            }
        }

        private void Menu_Undo(object sender, RoutedEventArgs e)
        {
            Command_Undo();
        }

        private void Menu_Redo(object sender, RoutedEventArgs e)
        {
            Command_Redo();
        }

        private void Menu_Cut(object sender, RoutedEventArgs e)
        {
            Command_Cut();
        }

        private void Menu_Copy(object sender, RoutedEventArgs e)
        {
            Command_Copy();
        }

        private void Menu_Paste(object sender, RoutedEventArgs e)
        {
            Command_Paste();
        }

        private void Menu_ExpandAll(object sender, RoutedEventArgs e)
        {
            Command_FlushFoldingState(false);
        }

        private void Menu_CollapseAll(object sender, RoutedEventArgs e)
        {
            Command_FlushFoldingState(true);
        }

        private void Menu_JumpTo(object sender, RoutedEventArgs e)
        {
            Command_GoToLine();
        }

        private void Menu_CommentLine(object sender, RoutedEventArgs e)
        {
            Command_ToggleCommentLine(true);
        }

        private void Menu_UncommentLine(object sender, RoutedEventArgs e)
        {
            Command_ToggleCommentLine(false);
        }

        private void Menu_SelectAll(object sender, RoutedEventArgs e)
        {
            Command_SelectAll();
        }

        private void Menu_FindAndReplace(object sender, RoutedEventArgs e)
        {
            Command_FindReplace();
        }

        private void Menu_CompileAll(object sender, RoutedEventArgs e)
        {
            Compile_SPScripts();
        }

        private void Menu_Compile(object sender, RoutedEventArgs e)
        {
            Compile_SPScripts(false);
        }

        private void Menu_CopyPlugin(object sender, RoutedEventArgs e)
        {
            Copy_Plugins();
        }

        private void Menu_FTPUpload(object sender, RoutedEventArgs e)
        {
            FTPUpload_Plugins();
        }

        private void Menu_StartServer(object sender, RoutedEventArgs e)
        {
            Server_Start();
        }

        private void Menu_SendRCon(object sender, RoutedEventArgs e)
        {
            Server_Query();
        }

        private void Menu_OpenWebsiteFromTag(object sender, RoutedEventArgs e)
        {
            var url = (string)((MenuItem)sender).Tag;
            Process.Start(new ProcessStartInfo(url));
        }

        private void Menu_About(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow { Owner = this, ShowInTaskbar = false };
            aboutWindow.ShowDialog();
        }

        private void Menu_Help(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(Constants.GitHubWiki));
        }

        private void Menu_OpenSPDef(object sender, RoutedEventArgs e)
        {
            var spDefinitionWindow = new SPDefinitionWindow { Owner = this, ShowInTaskbar = false };
            spDefinitionWindow.ShowDialog();
        }

        private void Menu_OpenOptions(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new OptionsWindow { Owner = this, ShowInTaskbar = false };
            optionsWindow.ShowDialog();
        }

        private void Menu_ReFormatCurrent(object sender, RoutedEventArgs e)
        {
            Command_TidyCode(false);
        }

        private void Menu_ReFormatAll(object sender, RoutedEventArgs e)
        {
            Command_TidyCode(true);
        }

        private void Menu_DecompileLysis(object sender, RoutedEventArgs e)
        {
            Command_Decompile();
        }

        private void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(Constants.GitHubNewIssueLink));
        }

        private async void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            var updatingWindow = await this.ShowProgressAsync(Program.Translations.Get("CheckingUpdates") + "...", "", false, MetroDialogOptions);
            updatingWindow.SetIndeterminate();

            await UpdateCheck.Check();
            var status = Program.UpdateStatus;
            if (status.IsAvailable)
            {
                await updatingWindow.CloseAsync();
                var uWindow = new UpdateWindow(status) { Owner = this };
                uWindow.ShowDialog();
                if (uWindow.Succeeded)
                {
                    Command_SaveAll();
                    lock (Program.UpdateStatus)
                    {
                        Program.UpdateStatus.WriteAble = false;
                        Program.UpdateStatus.IsAvailable = false;
                    }

                    Close();
                }
            }
            else
            {
                await updatingWindow.CloseAsync();
                if (status.GotException)
                {
                    await this.ShowMessageAsync(Program.Translations.Get("FailedCheck"),
                        Program.Translations.Get("ErrorUpdate") + Environment.NewLine +
                        $"{Program.Translations.Get("Details")}: " + status.ExceptionMessage
                        , MessageDialogStyle.Affirmative, MetroDialogOptions);
                }
                else
                {
                    await this.ShowMessageAsync(Program.Translations.Get("VersUpToDate"),
                        string.Format(Program.Translations.Get("VersionYour"),
                            Assembly.GetEntryAssembly()?.GetName().Version)
                        , MessageDialogStyle.Affirmative, MetroDialogOptions);
                }
            }
        }

        private async void Changelog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = await this.ShowProgressAsync("Retrieving changelog...", "Please wait...");
            dialog.SetIndeterminate();

            await UpdateCheck.Check();
            var status = Program.UpdateStatus;

            await dialog.CloseAsync();
            var uw = new UpdateWindow(status, true) { Owner = this };
            uw.ShowDialog();
        }

        private void MenuButton_Compile(object sender, RoutedEventArgs e)
        {
            Compile_SPScripts(CompileButton.SelectedIndex != 1);
        }

        private void MenuButton_Action(object sender, RoutedEventArgs e)
        {
            switch (CActionButton.SelectedIndex)
            {
                case 0: Copy_Plugins(); break;
                case 1: FTPUpload_Plugins(); break;
                case 2: Server_Start(); break;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads the input gesture texts to the menu items.
        /// </summary>
        private void LoadInputGestureTexts()
        {
            // Welcome to foreach hell
            foreach (var parentItem in MenuItems)
            {
                foreach (var child in parentItem.Items)
                {
                    if (child is MenuItem subChild)
                    {
                        foreach (var hkItem in Program.HotkeysList)
                        {
                            // Assign InputGestureText to all items
                            if (subChild.Name == $"MenuI_{hkItem.Command}")
                            {
                                subChild.InputGestureText = hkItem.Hotkey.ToString() == "None" ? string.Empty : hkItem.Hotkey.ToString();
                                break;
                            }
                            // Also assign InputGestureText to the stock restricted commands
                            else
                            {
                                foreach (var hk in HotkeyControl.RestrictedHotkeys)
                                {
                                    if (subChild.Name == $"MenuI_{hk.Key}")
                                    {
                                        subChild.InputGestureText = hk.Value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddNewRecentFile(FileInfo fInfo)
        {
            if (!Program.OptionsObject.RecentFiles.Any(x => x.Equals(fInfo.FullName)))
            {
                MenuI_Recent.Items.Insert(0, BuildRecentFileItem(fInfo.FullName));
                Program.OptionsObject.RecentFiles.AddFirst(fInfo.FullName);
                MenuI_Recent.IsEnabled = true;
                if (MenuI_Recent.Items.Count > 10)
                {
                    Program.OptionsObject.RecentFiles.RemoveLast();
                    MenuI_Recent.Items.RemoveAt(10);
                }
            }
        }

        private void LoadRecentsList()
        {
            var recentsList = Program.OptionsObject.RecentFiles;

            if (recentsList.Count == 0)
            {
                MenuI_Recent.IsEnabled = false;
                return;
            }

            foreach (var file in recentsList)
            {
                MenuI_Recent.Items.Add(BuildRecentFileItem(file));
            }
        }

        private MenuItem BuildRecentFileItem(string file)
        {
            // Create FileInfo to handle file
            var fInfo = new FileInfo(file);

            // Create the image for the file
            var img = new Image()
            {
                Source = new BitmapImage(new Uri($"/SPCode;component/Resources/Icons/{FileIcons[fInfo.Extension]}", UriKind.Relative)),
                Width = 15,
                Height = 15
            };

            // Create the text that the MenuItem will display
            var text = new TextBlock()
            {
                Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
            };
            text.Inlines.Add($"{fInfo.Name}  ");
            text.Inlines.Add(new Run(fInfo.FullName)
            {
                FontSize = FontSize - 2,
                Foreground = new SolidColorBrush(Colors.DarkGray),
                FontStyle = FontStyles.Italic
            });

            // Create the StackPanel where we will place image and text
            var stack = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };
            stack.Children.Add(img);
            stack.Children.Add(text);

            // Create the MenuItem and set the header to the created StackPanel
            var mi = new MenuItem()
            {
                Header = stack
            };

            // Set the click callback to open the file
            mi.Click += (sender, e) =>
            {
                TryLoadSourceFile(fInfo.FullName, out _, true, false, true);
            };

            // Return the MenuItem
            return mi;
        }
        #endregion
    }
}