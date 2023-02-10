using System.Linq;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI;

public partial class MainWindow
{
    public void Language_Translate()
    {
        CompileButtonDict = new () { Translate("CompileAll"), Translate("CompileCurrent") };
        ActionButtonDict = new () { Translate("Copy"), Translate("UploadFTP"), Translate("StartServer") };
        ((MenuItem)ConfigMenu.Items[ConfigMenu.Items.Count - 1]).Header = Translate("EditConfig");
        if (EditorReferences.Any())
        {
            foreach (var editor in EditorReferences)
            {
                editor.Language_Translate();
            }
        }
        MenuI_New.Header = Translate("New");
        MenuI_NewTemplate.Header = Translate("NewFromTemplate");
        MenuI_Open.Header = Translate("Open");
        MenuI_Recent.Header = Translate("RecentFiles");
        MenuI_ClearRecent.Header = Translate("ClearRecent");
        MenuI_ReopenLastClosedTab.Header = Translate("ReopenLastClosedTab");
        MenuI_Save.Header = Translate("Save");
        MenuI_SaveAll.Header = Translate("SaveAll");
        MenuI_SaveAs.Header = Translate("SaveAs");
        MenuI_Close.Header = Translate("Close");
        MenuI_CloseAll.Header = Translate("CloseAll");

        MenuI_File.Header = Translate("File");
        MenuI_Edit.Header = Translate("Edit");
        MenuI_Undo.Header = Translate("Undo");
        MenuI_Redo.Header = Translate("Redo");
        MenuI_Cut.Header = Translate("Cut");
        MenuI_Copy.Header = Translate("Copy");
        MenuI_Paste.Header = Translate("Paste");
        MenuI_Folding.Header = Translate("Folding");
        MenuI_FoldingsExpand.Header = Translate("FoldingsExpand");
        MenuI_FoldingsCollapse.Header = Translate("FoldingsCollapse");
        MenuI_GoToLine.Header = Translate("GoToLine");
        MenuI_CommentLine.Header = Translate("CommentLine");
        MenuI_UncommentLine.Header = Translate("UncommentLine");
        MenuI_SelectAll.Header = Translate("SelectAll");
        MenuI_SearchReplace.Header = Translate("SearchReplace");

        MenuI_Build.Header = Translate("Build");
        MenuI_CompileAll.Header = Translate("CompileAll");
        MenuI_CompileCurrent.Header = Translate("CompileCurrent");
        MenuI_CopyPlugins.Header = Translate("CopyPlugins");
        MenuI_UploadFTP.Header = Translate("UploadFTP");
        MenuI_StartServer.Header = Translate("StartServer");
        MenuI_SendRCON.Header = Translate("SendRCON");

        ConfigMenu.Header = Translate("Config");

        MenuI_Tools.Header = Translate("Tools");
        OptionMenuEntry.Header = Translate("Options");
        MenuI_SPAPI.Header = Translate("SourcepawnAPI");
        MenuI_SearchDefinition.Header = Translate("SearchDefinition");
        MenuI_NewApiWeb.Header = Translate("NewAPIWeb");
        MenuI_BetaApiWeb.Header = Translate("BetaAPIWeb");
        MenuI_Reformatter.Header = Translate("Reformatter");
        MenuI_ReformatCurrent.Header = Translate("ReformatCurrent");
        MenuI_ReformatAll.Header = Translate("ReformatAll");
        MenuI_Decompile.Header = Translate("Decompile");
        MenuI_ReportBugGit.Header = Translate("ReportBugGit");
        MenuI_Changelog.Header = Translate("OpenChangelog");
        UpdateCheckItem.Header = Translate("CheckUpdates");
        MenuI_Help.Header = Translate("Help");
        MenuI_About.Header = Translate("About");

        MenuC_FileName.Header = Translate("FileName");
        MenuC_Line.Header = Translate("Line");
        MenuC_Type.Header = Translate("Type");
        MenuC_Details.Header = Translate("Details");

        OBItemText_File.Text = Translate("OBTextFile");
        OBItemText_Config.Text = Translate("OBTextConfig");

        ((LogTextbox.Resources["LogContextMenu"] as ContextMenu).Items[0] as MenuItem).Header = Translate("ClearLogs");

        foreach (var child in FirstToolbar.Items)
        {
            if (child is Button btn)
            {
                btn.ToolTip = Translate(btn.Name.Substring(3));
            }
        }

        foreach (var child in SecondToolbar.Items)
        {
            if (child is Button btn)
            {
                btn.ToolTip = Translate(btn.Name.Substring(3));
            }
        }

        TextBoxHelper.SetWatermark(OBSearch, Translate("SearchFiles"));
        TxtSearchResults.Text = Translate("SearchResults");

        BtExpandCollapse.ToolTip = Translate(OBExpanded ? "ExpandAllDirs" : "CollapseAllDirs");
        BtRefreshDir.ToolTip = Translate("RefreshOB");

        Status_ErrorText.Text = string.Format(Translate("status_errors"), "0");
        Status_WarningText.Text = string.Format(Translate("status_warnings"), "0");
        Status_CopyErrorsButton.Content = Translate("CopyErrors");
    }
}