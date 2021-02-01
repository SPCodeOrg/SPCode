using System.IO;

namespace Lysis
{
    static class DebugSpew
    {
        public static void DumpGraph(LBlock[] blocks, TextWriter tw)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                tw.WriteLine("Block " + i + ": (" + blocks[i].pc + ")");
                for (int j = 0; j < blocks[i].instructions.Length; j++)
                {
                    tw.Write("  ");
                    blocks[i].instructions[j].print(tw);
                    tw.Write("\n");
                }
                tw.WriteLine("\n");
            }
        }
    }
}
