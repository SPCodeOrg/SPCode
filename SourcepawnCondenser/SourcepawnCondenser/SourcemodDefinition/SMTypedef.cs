namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMTypedef : SMBaseDefinition
    {
        public readonly string FullName;

        public SMTypedef(int index, int length, string file, string name, string commentString, string fullName) : base(index, length, file, name, commentString)
        {
            FullName = fullName;
        }
    }
}
