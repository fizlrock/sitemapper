using Microsoft.Msagl;


namespace LinkProcessor;


public class TreeBuilder
{
    private string[]? links;


    public void getMSGraph()
    {
    }

    public void buildTree()
    {
        if (links != null)
        {
						string root = links[0];
            
        }
    }


    public void loadLinks(string[] links)
    {
        var temp = links.Where(x => x.Length > 2);

        temp = temp
            .Where(x => x.StartsWith("http"))
            .Select(x => x.Split("//")[1]);

        temp = temp.Select(x => x.TrimEnd('/'));

        temp = temp.OrderBy(l => l.Length).Select(x => x);
				var t2 = temp.ToHashSet();
        this.links = t2.ToArray();
    }

    public void loadLinksFromFile(string path)
    {
        string raw;
        if (File.Exists(path))
        {
            using (TextReader textReader = File.OpenText(path))
                raw = textReader.ReadToEnd();
            links = raw.Split(System.Environment.NewLine);
            loadLinks(links);
        }
        else
            throw new FileNotFoundException();
    }

    public TreeBuilder()
    {
    }

}
