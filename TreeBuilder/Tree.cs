using System.Text;

namespace TreeBuilder;

public static class TreeUtils
{
    public static string getTreeReport(Node n)
    {
        List<string> buffer = new List<string>();
				putReportToBuffer(n, 0, buffer);

        return String.Join("\n", buffer);
    }

    private static void putReportToBuffer(Node n, int indent, List<string> buffer)
    {
        buffer.Add(Tabs(indent) + n.Value);
        foreach (Node child in n.Childs)
            putReportToBuffer(child, indent + 1, buffer);
    }

    static string Tabs(int n) => new string('\t', n);
}


public class Node
{
    //public Node? Father { get; init; }
    public string Value { get; init; } = "";
    public List<Node> Childs { get; init; } = new List<Node>();
}
