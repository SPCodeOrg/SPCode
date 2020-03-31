using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using Spedit.UI.Components;
using Timer = System.Timers.Timer;

namespace Spedit.UI
{
    public partial class MainWindow
    {
        private Thread backgroundParserThread;
        private ACNode[] currentACNodes;
        private ISNode[] currentISNodes;
        private SMDefinition currentSMDef;
        private ulong currentSMDefUID;

        private SMFunction[] currentSMFunctions;
        private Timer parseDistributorTimer;

        private void StartBackgroundParserThread()
        {
            backgroundParserThread = new Thread(BackgroundParser_Worker);
            backgroundParserThread.Start();
            parseDistributorTimer = new Timer(500.0);
            parseDistributorTimer.Elapsed += ParseDistributorTimer_Elapsed;
            parseDistributorTimer.Start();
        }

        private void ParseDistributorTimer_Elapsed(object sender, ElapsedEventArgs args)
        {
            if (currentSMDefUID == 0) return;

            EditorElement[] ee = null;
            EditorElement ce = null;
            Dispatcher?.Invoke(() =>
            {
                ee = GetAllEditorElements();
                ce = GetCurrentEditorElement();
            });
            if (ee == null || ce == null) return;

            Debug.Assert(ee != null, nameof(ee) + " != null");
            // ReSharper disable once PossibleNullReferenceException
            foreach (var e in ee)
                if (e.LastSMDefUpdateUID < currentSMDefUID) //wants an update of the SMDefinition
                {
                    if (e == ce)
                    {
                        Debug.Assert(ce != null, nameof(ce) + " != null");
                        // ReSharper disable once PossibleNullReferenceException
                        if (ce.ISAC_Open) continue;
                    }

                    e.InterruptLoadAutoCompletes(currentSMDef.FunctionStrings, currentSMFunctions, currentACNodes,
                        currentISNodes);
                    e.LastSMDefUpdateUID = currentSMDefUID;
                }
        }

        private void BackgroundParser_Worker()
        {
            while (true)
            while (Program.OptionsObject.Program_DynamicISAC)
            {
                Thread.Sleep(1000);
                var ee = GetAllEditorElements();
                var caret = -1;
                var text = string.Empty;
                
                if (ee != null)
                {
                    var definitions = new SMDefinition[ee.Length];
                    for (var i = 0; i < ee.Length; ++i)
                    {
                        var fInfo = new FileInfo(ee[i].FullFilePath);
                        if (fInfo.Extension.Trim('.').ToLowerInvariant() == "inc")
                            definitions[i] =
                                new Condenser(File.ReadAllText(fInfo.FullName), fInfo.Name).Condense();

                        if (fInfo.Extension.Trim('.').ToLowerInvariant() == "sp")
                        {
                            var i1 = i;
                            Dispatcher.Invoke(() =>
                            {
                                if (ee[i1].IsLoaded)
                                {
                                    caret = ee[i1].editor.CaretOffset;
                                    text = ee[i1].editor.Text;
                                    definitions[i1] =
                                        new Condenser(File.ReadAllText(fInfo.FullName), fInfo.Name)
                                            .Condense();
                                }
                            });
                        }
                    }

                    currentSMDef = Program.Configs[Program.SelectedConfig].GetSMDef()
                        .ProduceTemporaryExpandedDefinition(definitions, caret, text);
                    currentSMFunctions = currentSMDef.Functions.ToArray();
                    currentACNodes = currentSMDef.ProduceACNodes();
                    currentISNodes = currentSMDef.ProduceISNodes();
                    ++currentSMDefUID;
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }
    }
}