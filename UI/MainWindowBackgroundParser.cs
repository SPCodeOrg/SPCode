using System.Diagnostics;
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using System.Threading;
using System.Timers;
using System.IO;
using Spedit.UI.Components;

namespace Spedit.UI
{
	public partial class MainWindow
	{
		private ulong currentSMDefUID;
		Thread backgroundParserThread;
		SMDefinition currentSMDef;
		System.Timers.Timer parseDistributorTimer;

		private void StartBackgroundParserThread()
		{
			backgroundParserThread = new Thread(BackgroundParser_Worker);
			backgroundParserThread.Start();
			parseDistributorTimer = new System.Timers.Timer(500.0);
			parseDistributorTimer.Elapsed += ParseDistributorTimer_Elapsed;
			parseDistributorTimer.Start();
		}

		private void ParseDistributorTimer_Elapsed(object sender, ElapsedEventArgs args)
		{
			if (currentSMDefUID == 0) { return; }
			EditorElement[] ee = null;
			EditorElement ce = null;
			Dispatcher?.Invoke(() =>
			{
				ee = GetAllEditorElements();
				ce = GetCurrentEditorElement();
			});
			if (ee == null || ce == null) { return; } //this can happen!

			Debug.Assert(ee != null, nameof(ee) + " != null");
			foreach (var e in ee)
			{
				if (e.LastSMDefUpdateUID < currentSMDefUID) //wants an update of the SMDefinition
				{
					if (e == ce)
					{
						Debug.Assert(ce != null, nameof(ce) + " != null");
						if (ce.ISAC_Open)
						{
							continue;
						}
					}
					e.InterruptLoadAutoCompletes(currentSMDef.FunctionStrings, currentSMFunctions, currentACNodes, currentISNodes);
					e.LastSMDefUpdateUID = currentSMDefUID;
				}
			}
		}

		private SMFunction[] currentSMFunctions;
		private ACNode[] currentACNodes;
		private ISNode[] currentISNodes;

		private void BackgroundParser_Worker()
		{
			while (true)
			{
				while (Program.OptionsObject.Program_DynamicISAC)
				{
					Thread.Sleep(5000);
					var ee = GetAllEditorElements();
					if (ee != null)
					{
						SMDefinition[] definitions = new SMDefinition[ee.Length];
						for (int i = 0; i < ee.Length; ++i)
						{
							FileInfo fInfo = new FileInfo(ee[i].FullFilePath);
							if (fInfo.Extension.Trim('.').ToLowerInvariant() == "inc")
							{
								definitions[i] = (new Condenser(File.ReadAllText(fInfo.FullName), fInfo.Name).Condense());
							}
						}
						currentSMDef = (Program.Configs[Program.SelectedConfig].GetSMDef()).ProduceTemporaryExpandedDefinition(definitions);
						currentSMFunctions = currentSMDef.Functions.ToArray();
						currentACNodes = currentSMDef.ProduceACNodes();
						currentISNodes = currentSMDef.ProduceISNodes();
						++currentSMDefUID;
					}
				}
				Thread.Sleep(5000);
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}
}
