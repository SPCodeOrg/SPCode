using System;
using System.IO;
using System.Text;
using SourcePawn;

namespace Lysis
{
    public static class LysisDecompiler
    {
        public static string Analyze(FileInfo fInfo)
        {
            PawnFile file;
            try
            {
                file = PawnFile.FromFile(fInfo.FullName);
            }
            catch (Exception e)
            {
                return "Error while loading file." + Environment.NewLine + "Details: " + e.Message + Environment.NewLine + "Stacktrace: " + e.StackTrace;
            }
            var outString = new StringBuilder();

            SourceBuilder source;
            try
            {
                source = new SourceBuilder(file, outString);
            }
            catch (Exception e) //I admit: i have no clue if this can happen, i was too lazy to look.
            {
                return "Error while building source." + Environment.NewLine + "Details: " + e.Message + Environment.NewLine + "Stacktrace: " + e.StackTrace;
            }
            try
            {
                source.writeGlobals();
            }
            catch (Exception e)
            {
                outString.AppendLine();
                outString.AppendLine("Error while write Globals");
                outString.AppendLine("Details: " + e.Message);
                outString.AppendLine("Stacktrace: " + e.StackTrace);
            }
            for (var i = 0; i < file.functions.Length; i++)
            {
                var fun = file.functions[i];
                try
                {
                    DumpMethod((SourcePawnFile)file, source, fun.address);
                    outString.AppendLine();
                }
#if DEBUG
                catch (OpCodeNotKnownException e)
                {
                    outString.AppendLine();
                    outString.AppendLine("/* ERROR! " + e.Message + " */");
                    outString.AppendLine(" function \"" + fun.name + "\" (number " + i + ")");
                    source = new SourceBuilder((SourcePawnFile)file, outString);
                }
                catch (LogicChainConversionException e)
                {
                    outString.AppendLine();
                    outString.AppendLine("/* ERROR! " + e.Message + " */");
                    outString.AppendLine(" function \"" + fun.name + "\" (number " + i + ")");
                    source = new SourceBuilder((SourcePawnFile)file, outString);
                }
#else
                catch (Exception e)
                {
                    outString.AppendLine();
                    outString.AppendLine("/* ERROR! " + e.Message + " */");
                    outString.AppendLine(" function \"" + fun.name + "\" (number " + i + ")");
                    source = new SourceBuilder((SourcePawnFile)file, outString);
                }
#endif
            }
            var NoteString = @"/*" + Environment.NewLine + "** ATTENTION" + Environment.NewLine +
                "** THE PRODUCED CODE, IS NOT ABLE TO COMPILE!" + Environment.NewLine +
                "** THE DECOMPILER JUST TRIES TO GIVE YOU A POSSIBILITY" + Environment.NewLine +
                "** TO LOOK HOW A PLUGIN DOES IT'S JOB AND LOOK FOR" + Environment.NewLine +
                "** POSSIBLE MALICIOUS CODE." + Environment.NewLine + "**" + Environment.NewLine +
                "** ALL CONVERSIONS ARE WRONG! AT EXAMPLE:" + Environment.NewLine +
                "** SetEntityRenderFx(client, RenderFx 0);  →  SetEntityRenderFx(client, view_as<RenderFx>0);  →  SetEntityRenderFx(client, RENDERFX_NONE);" +
                Environment.NewLine + "*/" + Environment.NewLine + Environment.NewLine;
            outString.Insert(0, NoteString);
            return outString.ToString();
        }

        private static void DumpMethod(SourcePawnFile file, SourceBuilder source, uint addr)
        {
            var mp = new MethodParser(file, addr);
            var graph = mp.parse();

            var nb = new NodeBuilder(file, graph);
            var nblocks = nb.buildNodes();

            var ngraph = new NodeGraph(file, nblocks);

            // Remove dead phis first.
            NodeAnalysis.RemoveDeadCode(ngraph);

            var rewriter = new NodeRewriter(ngraph);
            rewriter.rewrite();

            NodeAnalysis.CollapseArrayReferences(ngraph);

            // Propagate type information.
            var ftypes = new ForwardTypePropagation(ngraph);
            ftypes.propagate();

            var btypes = new BackwardTypePropagation(ngraph);
            btypes.propagate();

            // We're not fixpoint, so just iterate again.
            ftypes.propagate();
            btypes.propagate();

            // Try this again now that we have type information.
            NodeAnalysis.CollapseArrayReferences(ngraph);

            ftypes.propagate();
            btypes.propagate();

            // Coalesce x[y] = x[y] + 5 into x[y] += 5
            NodeAnalysis.CoalesceLoadStores(ngraph);

            // After this, it is not legal to run type analysis again, because
            // arguments expecting references may have been rewritten to take
            // constants, for pretty-printing.
            NodeAnalysis.AnalyzeHeapUsage(ngraph);

            // Do another DCE pass, this time, without guards.
            NodeAnalysis.RemoveGuards(ngraph);
            NodeAnalysis.RemoveDeadCode(ngraph);

            var renamer = new NodeRenamer(ngraph);
            renamer.rename();

            // Do a pass to coalesce declaration+stores.
            NodeAnalysis.CoalesceLoadsAndDeclarations(ngraph);

            // Simplify conditional expressions.
            // BlockAnalysis.NormalizeConditionals(ngraph);
            var sb = new SourceStructureBuilder(ngraph);
            var structure = sb.build();

            source.write(structure);

            //System.Console.In.Read();
            //System.Console.In.Read();
        }

        private static Function FunctionByName(SourcePawnFile file, string name)
        {
            for (var i = 0; i < file.functions.Length; i++)
            {
                if (file.functions[i].name == name)
                {
                    return file.functions[i];
                }
            }
            return null;
        }

    }
}
