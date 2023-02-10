namespace SPCode.Utils;

public class ObjectBrowserTag
{
    public ObjectBrowserItemKind Kind;
    public string? Value;
}

public enum ObjectBrowserItemKind
{
    ParentDirectory,
    Directory,
    File,
    Empty
}