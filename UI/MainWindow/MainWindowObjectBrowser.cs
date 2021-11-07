using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MahApps.Metro.Controls.Dialogs;
using SPCode.UI.Windows;
using SPCode.Utils;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        #region Variables
        private string CurrentObjectBrowserDirectory = string.Empty;
        private readonly DispatcherTimer SearchCooldownTimer;
        private readonly List<TreeViewItem> ExpandedItems = new();
        private List<TreeViewItem> ExpandedItemsBuffer = new();
        private bool SearchMode = false;
        private bool OBExpanded = false;

        public readonly Dictionary<string, string> FileIcons = new()
        {
            { ".sp", Constants.PluginIcon },
            { ".inc", Constants.IncludeIcon },
            { ".txt", Constants.TxtIcon },
            { ".cfg", Constants.TxtIcon },
            { ".ini", Constants.TxtIcon },
            { ".smx", Constants.SmxIcon },
        };
        #endregion

        #region Events
        private void TreeViewOBItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.Source is not TreeViewItem item)
            {
                return;
            }
            var itemInfo = (ObjectBrowserTag)item.Tag;
            if (itemInfo.Kind != ObjectBrowserItemKind.Directory || !Directory.Exists(itemInfo.Value))
            {
                return;
            }
            OnExpandedItem(item);
        }

        private void TreeViewOBItem_Collapsed(object sender, RoutedEventArgs e)
        {
            if (e.Source is not TreeViewItem item)
            {
                return;
            }
            ExpandedItems.Remove(item);
        }

        private void TreeViewOBItem_RightClicked(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            var itemTag = treeViewItem?.Tag as ObjectBrowserTag;

            if (treeViewItem != null)
            {
                switch (itemTag.Kind)
                {
                    case ObjectBrowserItemKind.Directory:
                        treeViewItem.Focus();
                        ObjectBrowser.ContextMenu = ObjectBrowser.Resources["TVIContextMenuDir"] as ContextMenu;
                        break;

                    case ObjectBrowserItemKind.File when itemTag.Value.Substring(itemTag.Value.LastIndexOf('.')) == ".smx":
                        treeViewItem.Focus();
                        ObjectBrowser.ContextMenu = ObjectBrowser.Resources["TVIContextMenuSmx"] as ContextMenu;
                        break;

                    case ObjectBrowserItemKind.File:
                        treeViewItem.Focus();
                        ObjectBrowser.ContextMenu = ObjectBrowser.Resources["TVIContextMenu"] as ContextMenu;
                        break;

                    case ObjectBrowserItemKind.ParentDirectory:
                    case ObjectBrowserItemKind.Empty:
                        ObjectBrowser.ContextMenu = null;
                        break;
                }
            }
            e.Handled = true;
        }

        private void TreeViewOBItemParentDir_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }
            var currentInfo = new DirectoryInfo(CurrentObjectBrowserDirectory);
            var parentInfo = currentInfo.Parent;
            if (parentInfo != null)
            {
                if (parentInfo.Exists)
                {
                    ChangeObjectBrowserToDirectory(parentInfo.FullName);
                    return;
                }
            }
            ChangeObjectBrowserToDrives();
        }

        private void TreeViewOBItemFile_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || sender is not TreeViewItem item)
            {
                return;
            }
            var itemInfo = (ObjectBrowserTag)item.Tag;
            if (itemInfo.Kind == ObjectBrowserItemKind.File)
            {
                TryLoadSourceFile(itemInfo.Value, out _, true, false, true);
            }
        }

        private void OBItemOpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemFile = ((ObjectBrowser.SelectedItem as TreeViewItem)?.Tag as ObjectBrowserTag)?.Value;
            if (selectedItemFile != null)
            {
                Process.Start("explorer.exe", $"/select, \"{selectedItemFile}\"");
            }
        }

        private async void OBItemDecompile_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemFile = ((ObjectBrowser.SelectedItem as TreeViewItem)?.Tag as ObjectBrowserTag)?.Value;
            if (selectedItemFile != null)
            {
                await new DecompileUtil().DecompilePlugin(selectedItemFile);
            }
        }

        private void OBItemRename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ObjectBrowser.SelectedItem is TreeViewItem file)
                {
                    // Open up the Rename Window and fetch the new name from there
                    var fileTag = file.Tag as ObjectBrowserTag;
                    var renameWindow = new RenameWindow(fileTag.Value);
                    renameWindow.ShowDialog();

                    ObjectBrowser.ContextMenu = null;

                    // If we didn't receive an empty name...
                    if (!string.IsNullOrEmpty(renameWindow.NewName))
                    {
                        var oldFileInfo = new FileInfo(fileTag.Value);
                        var newFileInfo = new FileInfo(oldFileInfo.DirectoryName + @"\" + renameWindow.NewName);

                        // Rename file
                        File.Move(oldFileInfo.FullName, newFileInfo.FullName);

                        // If the new extension is not supported by SPCode, remove it from object browser
                        // else, rename and update the item
                        if (!FileIcons.ContainsKey(newFileInfo.Extension))
                        {
                            file.Visibility = Visibility.Collapsed;
                            return;
                        }
                        else
                        {
                            fileTag.Value = newFileInfo.FullName;
                            file.Header = BuildTreeViewItemContent(renameWindow.NewName, FileIcons[newFileInfo.Extension]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Program.Translations.Get("Error"), ex.Message);
            }
        }

        private void OBItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var file = ((ObjectBrowser.SelectedItem as TreeViewItem)?.Tag as ObjectBrowserTag)?.Value;
            if (file != null)
            {
                File.Delete(file);
                (ObjectBrowser.SelectedItem as TreeViewItem).Visibility = Visibility.Collapsed;
            }
        }

        private void ListViewOBItem_SelectFile(object sender, RoutedEventArgs e)
        {
            if (sender is not ListViewItem item)
            {
                return;
            }
            if (SearchMode)
            {
                OBSearch.Clear();
                HideSearchVisuals();
            }
            ObjectBrowser.ContextMenu = null;
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                var fInfo = new FileInfo(ee.FullFilePath);
                ChangeObjectBrowserToDirectory(fInfo.DirectoryName);
            }
            item.IsSelected = true;
            OBButtonHolder.SelectedIndex = -1;
        }

        private void ListViewOBItem_SelectConfig(object sender, RoutedEventArgs e)
        {
            if (sender is not ListViewItem item)
            {
                return;
            }
            if (SearchMode)
            {
                OBSearch.Clear();
                HideSearchVisuals();
            }
            ObjectBrowser.ContextMenu = null;
            var cc = Program.Configs[Program.SelectedConfig];
            if (cc.SMDirectories.Count > 0)
            {
                ChangeObjectBrowserToDirectory((string)OBDirList.SelectedItem);
            }
            item.IsSelected = true;
            OBButtonHolder.SelectedIndex = -1;
        }

        private void OBDirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OBSearch.Clear();
            if (SearchMode)
            {
                HideSearchVisuals();
            }
            ChangeObjectBrowserToDirectory((string)OBDirList.SelectedItem);
            OBButtonHolder.SelectedIndex = 1;
        }

        private void OBSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ObjectBrowser.ContextMenu = null;
            switch (e.Key)
            {
                case Key.Escape:
                    OBSearch.Clear();
                    HideSearchVisuals();
                    ChangeObjectBrowserToDirectory(CurrentObjectBrowserDirectory);
                    break;
                case Key.Enter:
                    if (SearchMode)
                    {
                        Search(OBSearch.Text);
                    }
                    break;
            }
        }

        private void OBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchCooldownTimer.Stop();
            SearchCooldownTimer.Start();
        }

        private void OnSearchCooldownTimerTick(object sender, EventArgs e)
        {
            SearchCooldownTimer.Stop();
            Search(OBSearch.Text);
        }

        private void BtExpandCollapse_Click(object sender, RoutedEventArgs e)
        {
            OBExpanded = !OBExpanded;
            MoveSubContainers(ObjectBrowser, OBExpanded);
            BtExpandCollapse.Content = (Image)FindResource(OBExpanded ? "ImgCollapse" : "ImgExpand");
            BtExpandCollapse.ToolTip = Program.Translations.Get(OBExpanded ? "CollapseAllDirs" : "ExpandAllDirs");
        }

        private void BtRefreshDir_Click(object sender, RoutedEventArgs e)
        {
            RefreshObjectBrowser();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Refreshes the object browser's items, remembering folding states.
        /// </summary>
        public void RefreshObjectBrowser()
        {
            // Delete context menu to prevent performing actions on potentially null elements
            ObjectBrowser.ContextMenu = null;

            // Refresh files from root directory - delete all files only
            foreach (TreeViewItem item in ObjectBrowser.Items)
            {
                if ((item.Tag as ObjectBrowserTag).Kind == ObjectBrowserItemKind.File)
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }

            // Get all items from root dir, and add only files
            var newRootItems = BuildDirectoryItems(CurrentObjectBrowserDirectory, out _).Where(x => (x.Tag as ObjectBrowserTag).Kind == ObjectBrowserItemKind.File).ToList();
            newRootItems.ForEach(x => ObjectBrowser.Items.Add(x));

            // Refresh all items remembering folding state
            ExpandedItemsBuffer = new List<TreeViewItem>(ExpandedItems);
            ExpandedItems.Clear();
            ExpandedItemsBuffer.ForEach(x => OnExpandedItem(x));
        }

        /// <summary>
        /// Gets a list of files based on the 'filter' search criteria to append as new items to the Object Browser.
        /// </summary>
        /// <param name="filter">The filter text</param>
        private void Search(string filter)
        {
            try
            {
                // Set up visuals
                if (string.IsNullOrWhiteSpace(filter))
                {
                    HideSearchVisuals();
                    ObjectBrowser.Items.Clear();
                    ChangeObjectBrowserToDirectory(CurrentObjectBrowserDirectory);
                    return;
                }

                ShowSearchVisuals();

                // Create list with all dirs, including the one we're standing on
                var dirs = new List<string>(Directory.GetDirectories(CurrentObjectBrowserDirectory, "*.*", SearchOption.AllDirectories));
                dirs.Insert(0, CurrentObjectBrowserDirectory);

                // Clear all items
                ObjectBrowser.Items.Clear();

                // Create List<TreeViewItem> with filter and add all items to TreeView
                foreach (var item in GetFiles(dirs, filter))
                {
                    ObjectBrowser.Items.Add(item);
                }
                if (ObjectBrowser.Items.Count == 0)
                {
                    ObjectBrowser.Items.Add(new TreeViewItem()
                    {
                        Header = BuildTreeViewItemContent($"{Program.Translations.Get("NoResultsThisDir")}", Constants.EmptyIcon),
                        FontStyle = FontStyles.Italic,
                        Foreground = new SolidColorBrush(Colors.Gray),
                        Tag = new ObjectBrowserTag()
                        {
                            Kind = ObjectBrowserItemKind.Empty
                        }
                    });
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Gets all files that match the search criteria specified in 'filter' in all the specified directories in 'dirs'.
        /// </summary>
        /// <param name="dirs">List of directories to search from</param>
        /// <param name="filter">Search criteria to compare against</param>
        /// <returns></returns>
        private List<TreeViewItem> GetFiles(List<string> dirs, string filter)
        {
            var list = new List<TreeViewItem>();

            foreach (var dir in dirs)
            {
                var files = Directory.GetFiles(dir)
                    .Where(x => new FileInfo(x).Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 && FileIcons.ContainsKey(x.Substring(x.LastIndexOf('.'))))
                    .ToList();
                foreach (var file in files)
                {
                    var fInfo = new FileInfo(file);
                    var tvi = new TreeViewItem()
                    {
                        Header = BuildTreeViewItemContent(fInfo.Name, FileIcons[fInfo.Extension], fInfo.FullName),
                        ToolTip = fInfo.FullName,
                        Tag = new ObjectBrowserTag()
                        {
                            Kind = ObjectBrowserItemKind.File,
                            Value = fInfo.FullName
                        }
                    };
                    tvi.MouseDoubleClick += TreeViewOBItemFile_DoubleClicked;
                    list.Add(tvi);
                }
            }
            return list;
        }

        /// <summary>
        /// Shows 'Search Results' title from the Object Browser and lowers TreeView height.
        /// </summary>
        private void ShowSearchVisuals()
        {
            if (SearchMode)
            {
                return;
            }
            var objMargin = ObjectBrowser.Margin;
            objMargin.Top += 30;
            ObjectBrowser.Margin = objMargin;
            TxtSearchResults.Visibility = Visibility.Visible;
            SearchMode = true;
        }

        /// <summary>
        /// Hides 'Search Results' title from the Object Browser and restores TreeView height.
        /// </summary>
        private void HideSearchVisuals()
        {
            if (!SearchMode)
            {
                return;
            }
            var objMargin = ObjectBrowser.Margin;
            objMargin.Top -= 30;
            ObjectBrowser.Margin = objMargin;
            TxtSearchResults.Visibility = Visibility.Hidden;
            SearchMode = false;
        }

        /// <summary>
        /// Helper function to fill all the contents of an expanded directory in the Object Browser.
        /// </summary>
        /// <param name="item">The directory kind received item, to resolve all its children.</param>
        private void OnExpandedItem(TreeViewItem item)
        {
            var itemInfo = (ObjectBrowserTag)item.Tag;
            ExpandedItems.Add(item);
            Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
            using (Dispatcher.DisableProcessing())
            {
                item.Items.Clear();
                var newItems = BuildDirectoryItems(itemInfo.Value, out var itemsToExpand);
                newItems.ForEach(x => item.Items.Add(x));
                itemsToExpand.ForEach(x => x.IsExpanded = true);
            }
        }

        /// <summary>
        /// Helper function to collapse or expand all of the items of the Object Browser.
        /// </summary>
        /// <param name="parentContainer">The TreeViewItem received to recursively expand or collapse everything inside it.</param>
        /// <param name="expand">Whether to expand or collapse the items.</param>
        private static void MoveSubContainers(ItemsControl parentContainer, bool expand)
        {
            foreach (var item in parentContainer.Items)
            {
                if (parentContainer.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem currentContainer && currentContainer.Items.Count > 0)
                {
                    // Expand the current item.
                    currentContainer.IsExpanded = expand;
                    if (currentContainer.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                    {
                        // If the sub containers of current item is not ready, we need to wait until
                        // they are generated.
                        currentContainer.ItemContainerGenerator.StatusChanged += delegate
                        {
                            MoveSubContainers(currentContainer, expand);
                        };
                    }
                    else
                    {
                        // If the sub containers of current item is ready, we can directly go to the next
                        // iteration to expand them.
                        MoveSubContainers(currentContainer, expand);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all Object Browser items and fills them with the specified directory's children (collapsing all expansions).
        /// </summary>
        /// <param name="dir">Directory to fetch contents from.</param>
        private void ChangeObjectBrowserToDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                var cc = Program.Configs[Program.SelectedConfig];
                if (cc.SMDirectories.Count > 0)
                {
                    dir = cc.SMDirectories[0];
                }
            }
            else if (dir == "0:")
            {
                ChangeObjectBrowserToDrives();
                return;
            }
            if (!Directory.Exists(dir))
            {
                dir = Environment.CurrentDirectory;
            }
            try
            {
                Directory.GetAccessControl(dir);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            CurrentObjectBrowserDirectory = dir;
            Program.OptionsObject.Program_ObjectBrowserDirectory = CurrentObjectBrowserDirectory;

            Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
            using (Dispatcher.DisableProcessing())
            {
                ObjectBrowser.Items.Clear();
                var parentDirItem = new TreeViewItem()
                {
                    Header = "..",
                    Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.ParentDirectory }
                };
                parentDirItem.MouseDoubleClick += TreeViewOBItemParentDir_DoubleClicked;
                parentDirItem.PreviewMouseRightButtonDown += TreeViewOBItem_RightClicked;
                ObjectBrowser.Items.Add(parentDirItem);
                var newItems = BuildDirectoryItems(dir, out _);
                foreach (var item in newItems)
                {
                    ObjectBrowser.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Similar funcionality described in ChangeObjectBrowserToDirectory, adapted to work on drives
        /// </summary>
        private void ChangeObjectBrowserToDrives()
        {
            Program.OptionsObject.Program_ObjectBrowserDirectory = "0:";
            var drives = DriveInfo.GetDrives();
            Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
            using (Dispatcher.DisableProcessing())
            {
                ObjectBrowser.Items.Clear();
                foreach (var dInfo in drives)
                {
                    if (dInfo.IsReady && (dInfo.DriveType == DriveType.Fixed || dInfo.DriveType == DriveType.Removable))
                    {
                        var tvi = new TreeViewItem()
                        {
                            Header = BuildTreeViewItemContent(dInfo.Name, Constants.FolderIcon),
                            Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.Directory, Value = dInfo.RootDirectory.FullName }
                        };
                        tvi.Items.Add("...");
                        ObjectBrowser.Items.Add(tvi);
                    }
                }
            }
        }

        /// <summary>
        /// <para> Helper function to build an expanded item's contents. </para>
        /// <para> It outs a TreeViewItem list to be used when using the Reload function to keep directories expanded after refreshing. </para>
        /// </summary>
        /// <param name="dir">Directory to fetch contents from.</param>
        /// <param name="itemsToExpand">List of items that were expanded before calling this function to reload the Object Browser items.</param>
        /// <returns>List of Items build from the specified directory.</returns>
        private List<TreeViewItem> BuildDirectoryItems(string dir, out List<TreeViewItem> itemsToExpand)
        {
            itemsToExpand = new();
            var itemList = new List<TreeViewItem>();

            // GetFiles() filter is not precise and doing new FileInfo(x).Extension is slower
            var directories = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            var incFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".inc")).ToList();
            var spFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".sp")).ToList();
            var smxFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".smx")).ToList();
            var txtFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".txt")).ToList();
            var cfgFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".cfg")).ToList();
            var iniFiles = Directory.GetFiles(dir).Where(x => x.Contains('.') && x.Substring(x.LastIndexOf('.')).Equals(".ini")).ToList();

            var itemsToAdd = new List<string>();
            itemsToAdd.AddRange(directories);
            itemsToAdd.AddRange(incFiles);
            itemsToAdd.AddRange(spFiles);
            itemsToAdd.AddRange(smxFiles);
            itemsToAdd.AddRange(txtFiles);
            itemsToAdd.AddRange(cfgFiles);
            itemsToAdd.AddRange(iniFiles);

            // If we have to build contents of an empty folder...
            if (itemsToAdd.Count == 0)
            {
                var tvi = new TreeViewItem()
                {
                    Header = BuildTreeViewItemContent($"({Program.Translations.Get("Empty").ToLower()})", Constants.EmptyIcon),
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Tag = new ObjectBrowserTag()
                    {
                        Kind = ObjectBrowserItemKind.Empty
                    }
                };
                itemList.Add(tvi);
                return itemList;
            }

            foreach (var item in itemsToAdd)
            {
                var attr = File.GetAttributes(item);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var dInfo = new DirectoryInfo(item);
                    if (!dInfo.Exists)
                    {
                        continue;
                    }
                    try
                    {
                        dInfo.GetAccessControl();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    var tvi = new TreeViewItem()
                    {
                        Header = BuildTreeViewItemContent(dInfo.Name, Constants.FolderIcon),
                        Tag = new ObjectBrowserTag()
                        {
                            Kind = ObjectBrowserItemKind.Directory,
                            Value = dInfo.FullName
                        }
                    };
                    if (ExpandedItemsBuffer.Any(x => ((ObjectBrowserTag)x.Tag).Value == dInfo.FullName))
                    {
                        itemsToExpand.Add(tvi);
                    }
                    // This is to trigger the "expandability" of the TreeViewItem, if it's a directory
                    tvi.Items.Add("");
                    itemList.Add(tvi);
                }
                else
                {
                    var fInfo = new FileInfo(item);
                    if (!fInfo.Exists)
                    {
                        continue;
                    }
                    var tvi = new TreeViewItem()
                    {
                        Header = BuildTreeViewItemContent(fInfo.Name, FileIcons[fInfo.Extension]),
                        Tag = new ObjectBrowserTag()
                        {
                            Kind = ObjectBrowserItemKind.File,
                            Value = fInfo.FullName
                        }
                    };
                    tvi.MouseDoubleClick += TreeViewOBItemFile_DoubleClicked;
                    tvi.MouseDown += TreeViewOBItem_RightClicked;
                    itemList.Add(tvi);
                }
            }
            return itemList;
        }

        /// <summary>
        /// Helper function to build the visuals of the TreeViewItem that's going to be created.
        /// </summary>
        /// <param name="headerString">The text of the item.</param>
        /// <param name="iconFile">Icon that will be displayed</param>
        /// <param name="path">Optional path specification to show next to the header</param>
        /// <seealso cref="Search(string)"/>
        /// <returns>Newly created StackPanel</returns>
        private StackPanel BuildTreeViewItemContent(string headerString, string iconFile, string path = "")
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            var image = new Image();
            var uriPath = $"/SPCode;component/Resources/Icons/{iconFile}";
            image.Source = new BitmapImage(new Uri(uriPath, UriKind.Relative));
            image.Width = 16;
            image.Height = 16;
            var lbl = new TextBlock
            {
                Margin = new Thickness(5.0, 0.0, 0.0, 0.0)
            };

            if (string.IsNullOrEmpty(path))
            {
                lbl.Text = headerString;
            }
            else
            {
                lbl.Inlines.Add($"{headerString}  ");
                lbl.Inlines.Add(new Run(path)
                {
                    Foreground = new SolidColorBrush(Colors.DarkGray),
                    FontStyle = FontStyles.Italic,
                    FontSize = FontSize - 2,
                });
                lbl.IsHitTestVisible = false;
            }
            stack.Children.Add(image);
            stack.Children.Add(lbl);
            return stack;
        }

        /// <summary>
        /// Helper function to retrieve a TreeViewItem while right-clicking it to enable context menu capabilities
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && source is not TreeViewItem)
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as TreeViewItem;
        }

        /// <summary>
        /// Disables the File tab button of the Object Browser if there's no file opened in the editor.
        /// </summary>
        public void UpdateOBFileButton()
        {
            if (GetAllEditorElements() == null && GetAllDASMElements() == null)
            {
                OBTabFile.IsEnabled = false;
                OBTabFile.IsSelected = false;
                OBTabConfig.IsSelected = true;
            }
            else
            {
                OBTabFile.IsEnabled = true;
            }
        }

        #endregion
    }
}