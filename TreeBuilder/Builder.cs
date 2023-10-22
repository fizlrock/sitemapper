
namespace TreeBuilder;


public static class Builder
{
    public static Node buildTree(IEnumerable<string> links)
    {

        string[][] splited_urls = links
                        .Where(x => !String.IsNullOrWhiteSpace(x))
                        .Select(x => x.Split("/"))
                        .ToArray();

        Node root = new Node()
        {
            Value = splited_urls[0][0]
        };

        foreach (var url in splited_urls)
            processLink(root, url, 1);

        return root;
    }

    private static void processLink(Node root, string[] link, int indent)
    {
        Node? child = root.Childs.Where(x => x.Value.Equals(link[indent])).FirstOrDefault();

        if (child == null)
        {
            child = new Node() { Value = link[indent] };
            root.Childs.Add(child);
        }

        if (indent < link.Length - 1)
            processLink(child, link, indent + 1);


    }
}
