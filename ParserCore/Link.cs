
using System.Diagnostics.CodeAnalysis;

namespace ParserCore;


public struct Link
{

    public string URL { get; init; }
    public LinkType Type { get; init; }
    public LinkContentType ContentType { get; init; }


    public override string? ToString()
    {
        return $"url: {URL}, type: {Type}, content_type: {ContentType}";
    }
}


public enum LinkType
{
    Domain, External
}

public enum LinkContentType
{
    Image, Unknown, None
}


