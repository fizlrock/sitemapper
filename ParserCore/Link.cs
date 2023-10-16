
namespace ParserCore;


public struct Link
{

    public string URL { get; init; }
    public LinkType Type { get; init; }
    public LinkContentType ContentType { get; init; }
}


public enum LinkType
{
    Domain, External
}

public enum LinkContentType
{
    Image, Unknown, None
}


