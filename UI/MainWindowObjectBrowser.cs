using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SPCode.UI.Components;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private string CurrentObjectBrowserDirectory = string.Empty;
        
        private void TreeViewOBItem_Expanded(object sender, RoutedEventArgs e)
        {
            var source = e.Source;
            if (source is not TreeViewItem)
            {
                return;
            }
            var item = (TreeViewItem)source;
            var itemInfo = (ObjectBrowserTag)item.Tag;
            if (itemInfo.Kind != ObjectBrowserItemKind.Directory || !Directory.Exists(itemInfo.Value))
            {
                return;
            }

            Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
            using (Dispatcher.DisableProcessing())
            {
                item.Items.Clear();
                var newItems = BuildDirectoryItems(itemInfo.Value);
                foreach (var i in newItems)
                {
                    item.Items.Add(i);
                }
            }
        }

        private void TreeViewOBItemParentDir_DoubleClicked(object sender, RoutedEventArgs e)
        {
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

        private void TreeViewOBItemFile_DoubleClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem item)
            {
                return;
            }
            var itemInfo = (ObjectBrowserTag)item.Tag;
            if (itemInfo.Kind == ObjectBrowserItemKind.File)
            {
                TryLoadSourceFile(itemInfo.Value, true, false, true);
            }
        }

        private void ListViewOBItem_SelectFile(object sender, RoutedEventArgs e)
        {
            if (sender is not ListViewItem item)
            {
                return;
            }
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                var fInfo = new FileInfo(ee.FullFilePath);
                ChangeObjectBrowserToDirectory(fInfo.DirectoryName);
            }
            item.IsSelected = true;
            ObjectBrowserButtonHolder.SelectedIndex = -1;
        }
        private void ListViewOBItem_SelectConfig(object sender, RoutedEventArgs e)
        {
            if (sender is not ListViewItem item)
            {
                return;
            }
            var cc = Program.Configs[Program.SelectedConfig];
            if (cc.SMDirectories.Count > 0)
            {
                ChangeObjectBrowserToDirectory(cc.SMDirectories[0]);
            }
            item.IsSelected = true;
            ObjectBrowserButtonHolder.SelectedIndex = -1;
        }

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
                ObjectBrowser.Items.Add(parentDirItem);
                var newItems = BuildDirectoryItems(dir);
                foreach (var item in newItems)
                {
                    ObjectBrowser.Items.Add(item);
                }
            }
        }

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
                            Header = BuildTreeViewItemContent(dInfo.Name, "iconmonstr-folder-13-16.png"),
                            Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.Directory, Value = dInfo.RootDirectory.FullName }
                        };
                        tvi.Items.Add("...");
                        ObjectBrowser.Items.Add(tvi);
                    }
                }
            }
        }

        private void ObjectBrowserDirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeObjectBrowserToDirectory((string)ObjectBrowserDirList.SelectedItem);
            ObjectBrowserButtonHolder.SelectedIndex = 1;
        }

        private List<TreeViewItem> BuildDirectoryItems(string dir)
        {
            var itemList = new List<TreeViewItem>();
            var spFiles = Directory.GetFiles(dir, "*.sp", SearchOption.TopDirectoryOnly);
            var incFiles = Directory.GetFiles(dir, "*.inc", SearchOption.TopDirectoryOnly);
            var directories = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            foreach (var d in directories)
            {
                var dInfo = new DirectoryInfo(d);
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
                    Header = BuildTreeViewItemContent(dInfo.Name, "iconmonstr-folder-13-16.png"),
                    Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.Directory, Value = dInfo.FullName }
                };
                tvi.Items.Add("...");
                itemList.Add(tvi);
            }
            foreach (var f in spFiles)
            {
                var fInfo = new FileInfo(f);
                if (!fInfo.Exists)
                {
                    continue;
                }
                var tvi = new TreeViewItem()
                {
                    Header = BuildTreeViewItemContent(fInfo.Name, "iconmonstr-file-5-16.png"),
                    Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.File, Value = fInfo.FullName }
                };
                tvi.MouseDoubleClick += TreeViewOBItemFile_DoubleClicked;
                itemList.Add(tvi);
            }
            foreach (var f in incFiles)
            {
                var fInfo = new FileInfo(f);
                if (!fInfo.Exists)
                {
                    continue;
                }
                var tvi = new TreeViewItem()
                {
                    Header = BuildTreeViewItemContent(fInfo.Name, "iconmonstr-file-8-16.png"),
                    Tag = new ObjectBrowserTag() { Kind = ObjectBrowserItemKind.File, Value = fInfo.FullName }
                };
                tvi.MouseDoubleClick += TreeViewOBItemFile_DoubleClicked;
                itemList.Add(tvi);
            }
            return itemList;
        }

        private object BuildTreeViewItemContent(string headerString, string iconFile)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            var image = new Image();
            var uriPath = $"/SPCode;component/Resources/{iconFile}";
            image.Source = new BitmapImage(new Uri(uriPath, UriKind.Relative));
            image.Width = 16;
            image.Height = 16;
            var lbl = new TextBlock { Text = headerString, Margin = new Thickness(2.0, 0.0, 0.0, 0.0) };
            stack.Children.Add(image);
            stack.Children.Add(lbl);
            return stack;
        }

        private class ObjectBrowserTag
        {
            public ObjectBrowserItemKind Kind;
            public string Value;
        }

        private enum ObjectBrowserItemKind
        {
            ParentDirectory,
            Directory,
            File
        }
    }
}
