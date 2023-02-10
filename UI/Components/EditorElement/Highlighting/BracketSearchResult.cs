namespace SPCode.Utils;

public class BracketSearchResult
{
    public int OpeningBracketOffset { get; private set; }

    public int OpeningBracketLength { get; private set; }

    public int ClosingBracketOffset { get; private set; }

    public int ClosingBracketLength { get; private set; }

    public int DefinitionHeaderOffset { get; set; }

    public int DefinitionHeaderLength { get; set; }

    public BracketSearchResult(int openingBracketOffset, int openingBracketLength,
                               int closingBracketOffset, int closingBracketLength)
    {
        OpeningBracketOffset = openingBracketOffset;
        OpeningBracketLength = openingBracketLength;
        ClosingBracketOffset = closingBracketOffset;
        ClosingBracketLength = closingBracketLength;
    }
}