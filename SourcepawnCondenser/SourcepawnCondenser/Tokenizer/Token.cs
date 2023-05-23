namespace SourcepawnCondenser.Tokenizer;

public class Token
{
    public Token(string value, TokenKind kind, int index)
    {
        Value = value;
        Kind = kind;
        Index = index;
        Length = value.Length;
    }
    public Token(char value, TokenKind kind, int index)
    {
        Value = value.ToString();
        Kind = kind;
        Index = index;
        Length = 1;
    }
    public string Value;
    public TokenKind Kind;
    public int Index;
    public int Length;
}