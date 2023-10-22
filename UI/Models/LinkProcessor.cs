using System;
using System.Linq;
using System.Collections.Generic;
using AvaloniaGraphControl;


namespace UI.Models;

public class LinkProcessor
{
    public Graph generateGraph(string[] links)
    {
        string[][] splited_urls = links
                            .Where(x => !String.IsNullOrWhiteSpace(x))
                            .Select(x => x.Split("/"))
                            .ToArray();


        var graph = new Graph();
				HashSet<string> unic_names = new HashSet<string>();

        if (splited_urls.Any())
        {
            int max_index = splited_urls.Select(x => x.Length).Max();
            int index = 0;
            while (index < max_index)
            {
                var src = splited_urls
                                .Where(x => x.Length > index)
                                .Select(x => x[index])
																.ToHashSet();
																

                foreach (string s in src)
                {
                    var definitons = splited_urls
                                    .Where(url => url.Length > index + 1)
                                    .Where(url => url[index].Equals(s))
                                    .Select(url => url[index + 1])
																		.ToHashSet();
                    foreach (string d in definitons)
                        graph.Edges.Add(new Edge(s, d));
                }
                index++;
            }

        }

        return graph;
    }


    private List<string> getNodes(string[][] urls, int level)
    {

        var roots = urls
            .Select(x => x[level])
            .Where(x => string.IsNullOrWhiteSpace(x))
                        .ToHashSet();

        throw new NotImplementedException();
    }
}
