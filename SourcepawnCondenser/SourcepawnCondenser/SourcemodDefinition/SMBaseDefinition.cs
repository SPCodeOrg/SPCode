using System.Dynamic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public abstract class SMBaseDefinition
    {
        protected SMBaseDefinition(int index, int length, string file, string name, string commentString)
        {
            Index = index;
            Length = length;
            File = file;
            Name = name;
            CommentString = commentString;
        }

        public int Index { get; }
        public int Length { get; }
        public string File { get; }
        public string Name { get; }
        public string CommentString { get; }
    }
}