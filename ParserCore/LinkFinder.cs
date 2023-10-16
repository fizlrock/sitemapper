using System.Text.RegularExpressions;

namespace ParserCore;


public class LinkFinder
{
    private static readonly string[] image_extensions = { ".png", ".PNG", ".jpeg", ".JPEG", ".webp", ".jpg", ".JPG", ".svg" };
    private static Regex validate_url_pattern = new Regex(@"^(?'protocol'https?:\/\/)?(?'domain'[a-zA-Z.0-9-_]+)(\/[a-zA-Z0-9-_?]+)*(?'filetype'\.\S{1,4})?\/?$");


    public HashSet<Link> findLinks(string html)
    {
        throw new NotImplementedException();
    }

    private List<Link> validateAndSortLinks(HashSet<string> urls, string root_domain, string source)
    {
        List<Link> links = new List<Link>();


        Match domain_m = validate_url_pattern.Match(root_domain);
        string trimed_domain_url = domain_m.Groups["domain"].Value;

        foreach (string url in urls)
        {
            Match urlm = validate_url_pattern.Match(url);
            LinkType type = LinkType.External;
            LinkContentType contentType = LinkContentType.None;

            if (urlm.Success)
            {
                string url_domain = urlm.Groups["domain"].Value;
                string extension = urlm.Groups["filetype"].Value;
                if (!String.IsNullOrWhiteSpace(url_domain) && url_domain == trimed_domain_url)
                    type = LinkType.Domain;

                if (!String.IsNullOrWhiteSpace(extension))
                {
                    contentType = LinkContentType.Unknown;
                    if (image_extensions.Contains(extension))
                        contentType = LinkContentType.Image;
                }
            }

            links.Add(new Link() { URL = url, Type = type, ContentType = contentType });
        }

        return links;
    }





    private HashSet<string> findAndFormatUrls(string html, string source)
    {

        const string absolute_url_pattern = @"(src|href|content)=""(?'url'(?'domain_with_protocol'(https?:\/\/)?(?:www\.)?([-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b))?(?:[-a-zA-Z0-9()@:%_\+.~#?&\/=]*))""";

        MatchCollection matches = null;
        matches = Regex.Matches(html, absolute_url_pattern);

        HashSet<string> raw_urls = matches.Select(m => m.Groups["url"].Value).ToHashSet();


        // Приведение вида ссылок относительного к абсолютному /abs => https://ssau.ru/abs
        var short_links = raw_urls.Where(x => ((x.Length > 2) && (x[0] == '/') && (x[1] != '/'))).ToHashSet();
        raw_urls.ExceptWith(short_links);
        raw_urls.UnionWith(short_links.Select(x => source + x));

        // Приведение //journal.ssau.ru к https://journal.ssau.ru
        var defected = raw_urls.Where(x => x.StartsWith("//")).ToHashSet();
        raw_urls.ExceptWith(defected);
        raw_urls.UnionWith(defected.Select(x => "https:" + x));

        // На всякий случай ещё несколько преобразований. да перебор. да работает. оптимизирую потом
        /*
var temp = raw_urls.Where(x => x.Length > 2);

temp = temp
    .Where(x => x.StartsWith("http"))
    .Select(x => x.Split("//")[1]);

temp = temp.Select(x => x.TrimEnd('/'));

temp = temp.OrderBy(l => l.Length).Select(x => x);
raw_urls = temp.ToHashSet();

        */
        const string validate_pattern = @"[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$";
        return raw_urls.Where(x => Regex.IsMatch(x, validate_pattern)).ToHashSet();
    }


}


