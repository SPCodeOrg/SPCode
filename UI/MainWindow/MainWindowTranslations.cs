using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SPCode.UI;

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
            CompileButtonDict = new ObservableCollection<string>() { Program.Translations.Get("CompileAll"), Program.Translations.Get("CompileCurrent") };
            ActionButtonDict = new ObservableCollection<string>() { Program.Translations.Get("Copy"), Program.Translations.Get("UploadFTP"), Program.Translations.Get("StartServer") };
            ((MenuItem)ConfigMenu.Items[ConfigMenu.Items.Count - 1]).Header = Program.Translations.Get("EditConfig");
            var ee = GetAllEditorElements();
            if (ee != null)
            {
                foreach (var t in ee)
                {
                    t?.Language_Translate();
                }
            }
        }
        MenuI_File.Header = Program.Translations.Get("FileStr");
        MenuI_New.Header = Program.Translations.Get("New");
        MenuI_NewTemplate.Header = Program.Translations.Get("NewTemplate");
        MenuI_Open.Header = Program.Translations.Get("Open");
        MenuI_Save.Header = Program.Translations.Get("Save");
        MenuI_SaveAll.Header = Program.Translations.Get("SaveAll");
        MenuI_SaveAs.Header = Program.Translations.Get("SaveAs");
        MenuI_Close.Header = Program.Translations.Get("Close");
        MenuI_CloseAll.Header = Program.Translations.Get("CloseAll");

        MenuI_File.Header = Program.Translations.Get("File");
        MenuI_Edit.Header = Program.Translations.Get("Edit");
        MenuI_Undo.Header = Program.Translations.Get("Undo");
        MenuI_Redo.Header = Program.Translations.Get("Redo");
        MenuI_Cut.Header = Program.Translations.Get("Cut");
        MenuI_Copy.Header = Program.Translations.Get("Copy");
        MenuI_Paste.Header = Program.Translations.Get("Paste");
        MenuI_Folding.Header = Program.Translations.Get("Folding");
        MenuI_FoldingsExpand.Header = Program.Translations.Get("FoldingsExpand");
        MenuI_FoldingsCollapse.Header = Program.Translations.Get("FoldingsCollapse");
        MenuI_GoToLine.Header = Program.Translations.Get("GoToLine");
        MenuI_CommentLine.Header = Program.Translations.Get("CommentLine");
        MenuI_SelectAll.Header = Program.Translations.Get("SelectAll");
        MenuI_SearchReplace.Header = Program.Translations.Get("SearchReplace");

        MenuI_Build.Header = Program.Translations.Get("Build");
        MenuI_CompileAll.Header = Program.Translations.Get("CompileAll");
        MenuI_CompileCurrent.Header = Program.Translations.Get("CompileCurrent");
        MenuI_CopyPlugins.Header = Program.Translations.Get("CopyPlugins");
        MenuI_UploadFTP.Header = Program.Translations.Get("UploadFTP");
        MenuI_StartServer.Header = Program.Translations.Get("StartServer");
        MenuI_SendRCON.Header = Program.Translations.Get("SendRCON");

        ConfigMenu.Header = Program.Translations.Get("Config");

        MenuI_Tools.Header = Program.Translations.Get("Tools");
        OptionMenuEntry.Header = Program.Translations.Get("Options");
        MenuI_SearchDefinition.Header = Program.Translations.Get("SearchDefinition");
        MenuI_NewApiWeb.Header = Program.Translations.Get("NewAPIWeb");
        MenuI_BetaApiWeb.Header = Program.Translations.Get("BetaAPIWeb");
        MenuI_Reformatter.Header = Program.Translations.Get("Reformatter");
        MenuI_ReformatCurrent.Header = Program.Translations.Get("ReformatCurrent");
        MenuI_ReformatAll.Header = Program.Translations.Get("ReformatAll");
        MenuI_Decompile.Header = $"{Program.Translations.Get("Decompile")} .smx (Lysis)";
        MenuI_ReportBugGit.Header = Program.Translations.Get("ReportBugGit");
        UpdateCheckItem.Header = Program.Translations.Get("CheckUpdates");
        MenuI_About.Header = Program.Translations.Get("About");

        MenuC_FileName.Header = Program.Translations.Get("FileName");
        MenuC_Line.Header = Program.Translations.Get("Line");
        MenuC_Type.Header = Program.Translations.Get("TypeStr");
        MenuC_Details.Header = Program.Translations.Get("Details");

        OBItemText_File.Text = Program.Translations.Get("OBTextFile");
        OBItemText_Config.Text = Program.Translations.Get("OBTextConfig");

        TxtSearchFiles.Text = Program.Translations.Get("SearchFiles") + "...";
        TxtSearchResults.Text = Program.Translations.Get("SearchResults");

        BtExpandCollapse.ToolTip = Program.Translations.Get(OBExpanded ? "ExpandAllDirs" : "CollapseAllDirs");
        BtRefreshDir.ToolTip = Program.Translations.Get("RefreshOB");
    }
}
