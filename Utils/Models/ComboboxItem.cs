namespace SPCode.Utils.Models;

public class ComboboxItem
{
    public string Text { get; set; }
    public string Value { get; set; }

    public override string ToString()
    {
        return Text;
    }
}