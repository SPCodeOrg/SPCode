using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace CondenserTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var str = new StringBuilder();
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(@"D:\AlliedModders\includes", "*.inc", SearchOption.AllDirectories));
            str.AppendLine(files.Count.ToString());
            foreach (var f in files)
            {
                str.AppendLine(File.ReadAllText(f));
            }
            ExpandBox.IsChecked = false;
            textBox.Text = str.ToString();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = textBox.Text;
            var watch = new Stopwatch();
            watch.Start();
            var tList = Tokenizer.TokenizeString(text, false);
            watch.Stop();
            var t = tList.ToArray();
            var tokenToTextLength = t.Length / (double)text.Length;
            var subTitle = watch.ElapsedMilliseconds + " ms  -  tokenL/textL: " + tokenToTextLength + "  (" + t.Length + " / " + text.Length + ")";
            tokenStack.Children.Clear();
            var i = 0;
            if (t.Length < 10000)
            {
                foreach (var token in t)
                {
                    ++i;
                    var g = new Grid() { Background = ChooseBackgroundFromTokenKind(token.Kind) };
                    g.Tag = token;
                    g.MouseLeftButtonUp += G_MouseLeftButtonUp;
                    g.HorizontalAlignment = HorizontalAlignment.Stretch;
                    g.Children.Add(new TextBlock() { Text = token.Kind + " - '" + token.Value + "'", IsHitTestVisible = false });
                    tokenStack.Children.Add(g);
                }
            }
            termTree.Items.Clear();
            watch.Reset();
            watch.Start();
            var c = new SourcepawnCondenser.Condenser(text, "");
            var def = c.Condense();
            watch.Stop();
            subTitle += "  -  condenser: " + watch.ElapsedMilliseconds + " ms";
            Title = subTitle;
            var expand = ExpandBox.IsChecked.Value;
            var functionItem = new TreeViewItem() { Header = "functions (" + def.Functions.Count + ")", IsExpanded = expand };
            foreach (var f in def.Functions)
            {
                var item = new TreeViewItem() { Header = f.Name, IsExpanded = expand };
                item.Tag = f;
                item.MouseLeftButtonUp += ItemFunc_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + f.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + f.Length });
                item.Items.Add(new TreeViewItem() { Header = "Kind: " + f.FunctionKind, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "ReturnType: " + f.ReturnType });
                item.Items.Add(new TreeViewItem() { Header = "Comment: >>" + f.CommentString + "<<", Background = Brushes.LightGray });
                for (var j = 0; j < f.Parameters.Length; ++j)
                {
                    item.Items.Add(new TreeViewItem() { Header = "Parameter " + (j + 1) + ": " + f.Parameters[j], Background = ((j + 1) % 2 == 0) ? Brushes.LightGray : Brushes.White });
                }
                functionItem.Items.Add(item);
            }
            termTree.Items.Add(functionItem);

            var enumItem = new TreeViewItem() { Header = "enums (" + def.Enums.Count + ")", IsExpanded = expand };
            foreach (var en in def.Enums)
            {
                var item = new TreeViewItem() { Header = string.IsNullOrWhiteSpace(en.Name) ? "no name" : en.Name, IsExpanded = expand };
                item.Tag = en;
                item.MouseLeftButtonUp += ItemEnum_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + en.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + en.Length });
                for (var j = 0; j < en.Entries.Length; ++j)
                {
                    item.Items.Add(new TreeViewItem() { Header = "Entry " + (j + 1) + ": " + en.Entries[j], Background = (j % 2 == 0) ? Brushes.LightGray : Brushes.White });
                }
                enumItem.Items.Add(item);
            }
            termTree.Items.Add(enumItem);

            var structItem = new TreeViewItem() { Header = "structs (" + def.Structs.Count + ")", IsExpanded = expand };
            foreach (var s in def.Structs)
            {
                var item = new TreeViewItem() { Header = string.IsNullOrWhiteSpace(s.Name) ? "no name" : s.Name, IsExpanded = expand };
                item.Tag = s;
                item.MouseLeftButtonUp += ItemStruct_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + s.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + s.Length });
                structItem.Items.Add(item);
            }
            termTree.Items.Add(structItem);

            var dItem = new TreeViewItem() { Header = "defines (" + def.Defines.Count + ")", IsExpanded = expand };
            foreach (var d in def.Defines)
            {
                var item = new TreeViewItem() { Header = d.Name, IsExpanded = expand };
                item.Tag = d;
                item.MouseLeftButtonUp += Itemppd_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + d.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + d.Length });
                dItem.Items.Add(item);
            }
            termTree.Items.Add(dItem);

            var cItem = new TreeViewItem() { Header = "constants (" + def.Constants.Count + ")", IsExpanded = expand };
            foreach (var cn in def.Constants)
            {
                var item = new TreeViewItem() { Header = cn.Name, IsExpanded = expand };
                item.Tag = cn;
                item.MouseLeftButtonUp += Itemc_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + cn.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + cn.Length });
                cItem.Items.Add(item);
            }
            termTree.Items.Add(cItem);

            var mItem = new TreeViewItem() { Header = "methodmaps (" + def.Methodmaps.Count + ")", IsExpanded = expand };
            foreach (var m in def.Methodmaps)
            {
                var item = new TreeViewItem() { Header = m.Name, IsExpanded = expand };
                item.Tag = m;
                item.MouseLeftButtonUp += ItemMM_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + m.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + m.Length });
                item.Items.Add(new TreeViewItem() { Header = "Type: " + m.Type, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "InheritedType: " + m.InheritedType });
                var subItem = new TreeViewItem() { Header = "Methods", Background = Brushes.LightGray };
                for (var j = 0; j < m.Methods.Count; ++j)
                {
                    var subSubItem = new TreeViewItem() { Header = m.Methods[j].Name, Background = (j % 2 == 0) ? Brushes.LightGray : Brushes.White };
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Index: " + m.Methods[j].Index });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Length: " + m.Methods[j].Length, Background = Brushes.LightGray });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Comment: >>" + m.Methods[j].CommentString + "<<" });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Return: " + m.Methods[j].ReturnType, Background = Brushes.LightGray });
                    var k = 0;
                    for (; k < m.Methods[j].MethodKind.Length; ++k)
                    {
                        subSubItem.Items.Add(new TreeViewItem() { Header = "MethodKind" + (k + 1) + ": " + m.Methods[j].MethodKind[k], Background = (k % 2 == 0) ? Brushes.LightGray : Brushes.White });
                    }
                    for (var l = 0; l < m.Methods[j].Parameters.Length; ++l)
                    {
                        ++k;
                        subSubItem.Items.Add(new TreeViewItem() { Header = "Parameter" + (l + 1) + ": " + m.Methods[j].Parameters[l], Background = (k % 2 == 0) ? Brushes.LightGray : Brushes.White });
                    }
                    subItem.Items.Add(subSubItem);
                }
                item.Items.Add(subItem);
                subItem = new TreeViewItem() { Header = "Fields" };
                for (var j = 0; j < m.Fields.Count; ++j)
                {
                    var subSubItem = new TreeViewItem() { Header = m.Fields[j].Name, Background = (j % 2 == 0) ? Brushes.LightGray : Brushes.White };
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Index: " + m.Fields[j].Index });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Length: " + m.Fields[j].Length, Background = Brushes.LightGray });
                    //subSubItem.Items.Add(new TreeViewItem() { Header = "Type: " + m.Fields[j].Type });
                    subItem.Items.Add(subSubItem);
                }
                item.Items.Add(subItem);
                mItem.Items.Add(item);
            }
            termTree.Items.Add(mItem);

            var eItem = new TreeViewItem() { Header = "EnumStructs (" + def.EnumStructs.Count + ")", IsExpanded = expand };
            foreach (var m in def.EnumStructs)
            {
                var item = new TreeViewItem() { Header = m.Name, IsExpanded = expand };
                item.Tag = m;
                item.MouseLeftButtonUp += ItemMM_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + m.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + m.Length });
                // item.Items.Add(new TreeViewItem() { Header = "Type: " + m.Type, Background = Brushes.LightGray });
                var subItem = new TreeViewItem() { Header = "Methods", Background = Brushes.LightGray };
                for (var j = 0; j < m.Methods.Count; ++j)
                {
                    var subSubItem = new TreeViewItem() { Header = m.Methods[j].Name, Background = (j % 2 == 0) ? Brushes.LightGray : Brushes.White };
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Index: " + m.Methods[j].Index });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Length: " + m.Methods[j].Length, Background = Brushes.LightGray });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Comment: >>" + m.Methods[j].CommentString + "<<" });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Return: " + m.Methods[j].ReturnType, Background = Brushes.LightGray });
                    var k = 0;
                    for (; k < m.Methods[j].MethodKind.Length; ++k)
                    {
                        subSubItem.Items.Add(new TreeViewItem() { Header = "MethodKind" + (k + 1) + ": " + m.Methods[j].MethodKind[k], Background = (k % 2 == 0) ? Brushes.LightGray : Brushes.White });
                    }
                    for (var l = 0; l < m.Methods[j].Parameters.Length; ++l)
                    {
                        ++k;
                        subSubItem.Items.Add(new TreeViewItem() { Header = "Parameter" + (l + 1) + ": " + m.Methods[j].Parameters[l], Background = (k % 2 == 0) ? Brushes.LightGray : Brushes.White });
                    }
                    subItem.Items.Add(subSubItem);
                }
                item.Items.Add(subItem);
                subItem = new TreeViewItem() { Header = "Fields" };
                for (var j = 0; j < m.Fields.Count; ++j)
                {
                    var subSubItem = new TreeViewItem() { Header = m.Fields[j].Name, Background = (j % 2 == 0) ? Brushes.LightGray : Brushes.White };
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Index: " + m.Fields[j].Index });
                    subSubItem.Items.Add(new TreeViewItem() { Header = "Length: " + m.Fields[j].Length, Background = Brushes.LightGray });
                    //subSubItem.Items.Add(new TreeViewItem() { Header = "Type: " + m.Fields[j].Type });
                    subItem.Items.Add(subSubItem);
                }
                item.Items.Add(subItem);
                eItem.Items.Add(item);
            }
            termTree.Items.Add(eItem);

            var vItem = new TreeViewItem() { Header = "Variables (" + def.Constants.Count + ")", IsExpanded = expand };
            foreach (var v in def.Variables)
            {
                var item = new TreeViewItem() { Header = v.Name, IsExpanded = expand };
                item.Tag = v;
                item.MouseLeftButtonUp += Itemc_MouseLeftButtonUp;
                item.Items.Add(new TreeViewItem() { Header = "Index: " + v.Index, Background = Brushes.LightGray });
                item.Items.Add(new TreeViewItem() { Header = "Length: " + v.Length });
                item.Items.Add(new TreeViewItem() { Header = "Type: " + v.Type });
                item.Items.Add(new TreeViewItem() { Header = "Value: " + v.Value });
                item.Items.Add(new TreeViewItem() { Header = "Size: " + string.Join(",", v.Size) });
                item.Items.Add(new TreeViewItem() { Header = "Dimensions: " + v.Dimensions });
                vItem.Items.Add(item);
            }
            termTree.Items.Add(vItem);
        }

        private void ItemFunc_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMFunction function)
                {
                    textBox.Focus();
                    textBox.Select(function.Index, function.Length);
                }
            }
        }

        private void ItemEnum_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMEnum @enum)
                {
                    textBox.Focus();
                    textBox.Select(@enum.Index, @enum.Length);
                }
            }
        }

        private void ItemStruct_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMStruct @struct)
                {
                    textBox.Focus();
                    textBox.Select(@struct.Index, @struct.Length);
                }
            }
        }

        private void Itemppd_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMDefine define)
                {
                    textBox.Focus();
                    textBox.Select(define.Index, define.Length);
                }
            }
        }

        private void Itemc_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMConstant constant)
                {
                    textBox.Focus();
                    textBox.Select(constant.Index, constant.Length);
                }
            }
        }

        private void ItemMM_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((TreeViewItem)sender).Tag;
            if (token != null)
            {
                if (token is SMMethodmap methodmap)
                {
                    textBox.Focus();
                    textBox.Select(methodmap.Index, methodmap.Length);
                }
            }
        }

        private void G_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var token = ((Grid)sender).Tag;
            if (token != null)
            {
                if (token is Token token1)
                {
                    textBox.Focus();
                    textBox.Select(token1.Index, token1.Length);
                }
            }
        }

        private Brush ChooseBackgroundFromTokenKind(TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.BraceClose:
                case TokenKind.BraceOpen: return Brushes.LightGray;
                case TokenKind.Character: return Brushes.LightSalmon;
                case TokenKind.EOF: return Brushes.LimeGreen;
                case TokenKind.Identifier: return Brushes.LightSteelBlue;
                case TokenKind.Number: return Brushes.LightSeaGreen;
                case TokenKind.ParenthesisClose:
                case TokenKind.ParenthesisOpen: return Brushes.LightSlateGray;
                case TokenKind.Quote: return Brushes.LightGoldenrodYellow;
                case TokenKind.EOL: return Brushes.Aqua;
                case TokenKind.SingleLineComment:
                case TokenKind.MultiLineComment: return Brushes.Honeydew;
                default: return Brushes.IndianRed;
            }
        }

        private void CaretPositionChangedEvent(object sender, RoutedEventArgs e)
        {
            CaretLabel.Content = textBox.CaretIndex + " / " + textBox.SelectionLength;
        }
    }
}
