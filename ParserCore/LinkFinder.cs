using System.Text.RegularExpressions;

namespace ParserCore;


public class LinkFinder
{
    private static readonly string[] image_extensions = { "png", "PNG", "jpeg", "JPEG", "webp", "jpg", "JPG", "svg" };
    private static Regex validate_url_regex = new Regex(@"[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$");
    private static Regex absolute_url_regex = new Regex(@"(src|href|content)=""(?'url'(?'domain_with_protocol'(https?:\/\/)?(?:www\.)?([-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b))?(?:[-a-zA-Z0-9()@:%_\+.~#?&\/=]*))""");


    

    public static List<Link> sortLinks(HashSet<string> links, string domain_short_link)
    {
        List<Link> sorted_links = new List<Link>();


        foreach (string url in links)
        {
            LinkType type = LinkType.External;
            LinkContentType contentType = LinkContentType.None;
            string maybe_domain = url.Split('/')[0];
            string extension = url.Split('.').Last();

            if (!String.IsNullOrWhiteSpace(maybe_domain) && maybe_domain == domain_short_link)
                type = LinkType.Domain;


            if (extension.Length < 5 && !extension.Equals("html"))
            {
                contentType = LinkContentType.Unknown;
                if (image_extensions.Contains(extension))
                    contentType = LinkContentType.Image;
            }

            sorted_links.Add(new Link() { URL = url, Type = type, ContentType = contentType });
        }

        return sorted_links;
    }





    public static HashSet<string> findAndFormatUrls(string html, string source)
    {
        MatchCollection matches = absolute_url_regex.Matches(html);

        HashSet<string> raw_urls = matches.Select(m => m.Groups["url"].Value).ToHashSet();



        // Приведение вида ссылок относительного к абсолютному /abs => https://ssau.ru/abs
        var short_links = raw_urls.Where(x => ((x.Length > 2) && (x[0] == '/') && (x[1] != '/'))).ToHashSet();
        raw_urls.ExceptWith(short_links);
        raw_urls.UnionWith(short_links.Select(x => source + x));

        // Приведение //journal.ssau.ru к https://journal.ssau.ru
        var defected = raw_urls.Where(x => x.StartsWith("//")).ToHashSet();
        raw_urls.ExceptWith(defected);
        raw_urls.UnionWith(defected.Select(x => "https:" + x));

        // Чистим дубли и мусор, приводим к стандартному виду
        var temp = raw_urls.Where(x => x.Length > 2);
        temp = temp
            .Where(x => x.StartsWith("http"))
            .Select(x => x.Split("//")[1]);

        temp = temp.Select(x => x.TrimEnd('/'));

        temp = temp.OrderBy(l => l.Length).Select(x => x);
        raw_urls = temp.ToHashSet();

        // raw_urls.Where(x => validate_url_regex.IsMatch(x)).ToHashSet();

        return raw_urls;
    }



    public static List<string> getDomainLinks(string html, string url, string domain_url)
    {
				//Console.WriteLine($"url: <{url}>, domain: <{domain_url}>");
        var links = findAndFormatUrls(html, url);
        var sorted_links = sortLinks(links, domain_url);


        //Console.WriteLine(String.Join("\n", links));

        var finded_domain_links = sorted_links
            .Where(l => l.Type == LinkType.Domain)
            .Where(l => l.ContentType == LinkContentType.None)
            .Select(l => l.URL);
				return finded_domain_links.ToList();



    }


}


