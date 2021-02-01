namespace SourcepawnCondenser.Tokenizer
{
    public class Token
    {
        public Token(string Value_, TokenKind Kind_, int Index_)
        {
            Value = Value_;
            Kind = Kind_;
            Index = Index_;
            Length = Value_.Length;
        }
        public Token(char Value_, TokenKind Kind_, int Index_)
        {
            Value = Value_.ToString();
            Kind = Kind_;
            Index = Index_;
            Length = 1;
        }
        public string Value;
        public TokenKind Kind;
        public int Index;
        public int Length;
    }
}
