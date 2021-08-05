using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        public void Language_Translate(bool Initial = false)
        {
            if (Initial && Program.Translations.IsDefault)
            {
                return;
            }
            if (!Initial)
            {
                compileButtonDict = new ObservableCollection<string>() { Program.Translations.GetLanguage("CompileAll"), Program.Translations.GetLanguage("CompileCurrent") };
                actionButtonDict = new ObservableCollection<string>() { Program.Translations.GetLanguage("Copy"), Program.Translations.GetLanguage("UploadFTP"), Program.Translations.GetLanguage("StartServer") };
                ((MenuItem)ConfigMenu.Items[ConfigMenu.Items.Count - 1]).Header = Program.Translations.GetLanguage("EditConfig");
                var ee = GetAllEditorElements();
                if (ee != null)
                {
                    foreach (var t in ee)
                    {
                        t?.Language_Translate();
                    }
                }
            }
            MenuI_File.Header = Program.Translations.GetLanguage("FileStr");
            MenuI_New.Header = Program.Translations.GetLanguage("New");
            MenuI_NewTemplate.Header = Program.Translations.GetLanguage("NewTemplate");
            MenuI_Open.Header = Program.Translations.GetLanguage("Open");
            MenuI_Save.Header = Program.Translations.GetLanguage("Save");
            MenuI_SaveAll.Header = Program.Translations.GetLanguage("SaveAll");
            MenuI_SaveAs.Header = Program.Translations.GetLanguage("SaveAs");
            MenuI_Close.Header = Program.Translations.GetLanguage("Close");
            MenuI_CloseAll.Header = Program.Translations.GetLanguage("CloseAll");

            MenuI_File.Header = Program.Translations.GetLanguage("File");
            MenuI_Edit.Header = Program.Translations.GetLanguage("Edit");
            MenuI_Undo.Header = Program.Translations.GetLanguage("Undo");
            MenuI_Redo.Header = Program.Translations.GetLanguage("Redo");
            MenuI_Cut.Header = Program.Translations.GetLanguage("Cut");
            MenuI_Copy.Header = Program.Translations.GetLanguage("Copy");
            MenuI_Paste.Header = Program.Translations.GetLanguage("Paste");
            MenuI_Folding.Header = Program.Translations.GetLanguage("Folding");
            MenuI_FoldingsExpand.Header = Program.Translations.GetLanguage("FoldingsExpand");
            MenuI_FoldingsCollapse.Header = Program.Translations.GetLanguage("FoldingsCollapse");
            MenuI_GoToLine.Header = Program.Translations.GetLanguage("GoToLine");
            MenuI_CommentLine.Header = Program.Translations.GetLanguage("CommentLine");
            MenuI_SelectAll.Header = Program.Translations.GetLanguage("SelectAll");
            MenuI_SearchReplace.Header = Program.Translations.GetLanguage("SearchReplace");

            MenuI_Build.Header = Program.Translations.GetLanguage("Build");
            MenuI_CompileAll.Header = Program.Translations.GetLanguage("CompileAll");
            MenuI_CompileCurrent.Header = Program.Translations.GetLanguage("CompileCurrent");
            MenuI_CopyPlugins.Header = Program.Translations.GetLanguage("CopyPlugins");
            MenuI_UploadFTP.Header = Program.Translations.GetLanguage("UploadFTP");
            MenuI_StartServer.Header = Program.Translations.GetLanguage("StartServer");
            MenuI_SendRCON.Header = Program.Translations.GetLanguage("SendRCON");

            ConfigMenu.Header = Program.Translations.GetLanguage("Config");

            MenuI_Tools.Header = Program.Translations.GetLanguage("Tools");
            OptionMenuEntry.Header = Program.Translations.GetLanguage("Options");
            MenuI_SearchDefinition.Header = Program.Translations.GetLanguage("SearchDefinition");
            MenuI_NewApiWeb.Header = Program.Translations.GetLanguage("NewAPIWeb");
            MenuI_BetaApiWeb.Header = Program.Translations.GetLanguage("BetaAPIWeb");
            MenuI_Reformatter.Header = Program.Translations.GetLanguage("Reformatter");
            MenuI_ReformatCurrent.Header = Program.Translations.GetLanguage("ReformatCurr");
            MenuI_ReformatAll.Header = Program.Translations.GetLanguage("ReformatAll");
            MenuI_Decompile.Header = $"{Program.Translations.GetLanguage("Decompile")} .smx (Lysis)";
            MenuI_ReportBugGit.Header = Program.Translations.GetLanguage("ReportBugGit");
            UpdateCheckItem.Header = Program.Translations.GetLanguage("CheckUpdates");
            MenuI_About.Header = Program.Translations.GetLanguage("About");

            MenuC_FileName.Header = Program.Translations.GetLanguage("FileName");
            MenuC_Line.Header = Program.Translations.GetLanguage("Line");
            MenuC_Type.Header = Program.Translations.GetLanguage("TypeStr");
            MenuC_Details.Header = Program.Translations.GetLanguage("Details");

            OBItemText_File.Text = Program.Translations.GetLanguage("OBTextFile");
            OBItemText_Config.Text = Program.Translations.GetLanguage("OBTextConfig");
        }
    }
}
