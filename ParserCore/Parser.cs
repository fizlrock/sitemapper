using System.Text.RegularExpressions;

namespace ParserCore;
public class Parser
{
    public Parser()
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2); ;
        Reset();
        last_request = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        PageLimit = 100;
        RPM = 60;

    }

    private ParserStatus _status;
    public ParserStatus status
    {
        get => _status;
        private set
        {
            _status = value;
            status_changed?.Invoke(value);
        }
    }

    public void Reset()
    {
        to_check = new Stack<string>();
        visited = new HashSet<string>();
        DomainTree = new HashSet<string>();
        DomainImages = new HashSet<string>();
        status = ParserStatus.Waiting;
        root_url = "";
        Graph = new List<Link>();

    }



    private static readonly string[] image_extensions = { ".png", ".PNG", ".jpeg", ".JPEG", ".webp", ".jpg", ".JPG", ".svg" };
    private static Regex validate_url_pattern = new Regex(@"^(?'protocol'https?:\/\/)?(?'domain'[a-zA-Z.0-9-_]+)(\/[a-zA-Z0-9-_?]+)*(?'filetype'\.\S{1,4})?\/?$");


    private Stack<string> to_check;
    private HashSet<string> visited;
    public List<Link> Graph { private set; get; }
    public HashSet<string> DomainTree { private set; get; }
    public HashSet<string> DomainImages { private set; get; }

    private HttpClient client;


    private long last_request;

    public delegate void UIMessage(string message);
    public delegate void StatusChangedMessage(ParserStatus status);

    private event UIMessage new_page_notifier;
    private event UIMessage warning_notifier;
    private event StatusChangedMessage status_changed;

    public void AddNewPageNotifier(UIMessage m) => new_page_notifier += m;
    public void AddWarningNotifier(UIMessage m) => warning_notifier += m;
    public void AddStatusChangedNotifier(StatusChangedMessage m) => status_changed += m;


    private string root_url;
    public string URL
    {
        set
        {
            if (status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            var m = validate_url_pattern.Match(value);
            if ((!m.Success || status == ParserStatus.Paused))
                throw new ArgumentException("Переданная строка не является URL");
            Reset();
            root_url = value;
            to_check.Push(root_url);
        }
        get => root_url;
    }

    private int page_limit;
    public int PageLimit
    {
        get => page_limit;
        set
        {
            if (status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во посещенных страниц должно быть больше нуля");
            page_limit = value;
        }
    }

    private int request_delay; // Время между запросами в мс
    public int RPM
    {
        get
        {
            return (int)(1 / ((double)request_delay / 60000));
        }
        set
        {
            if (status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во запросов в минуту должно быть больше нуля");
            request_delay = 60000 / value;
            Console.WriteLine($"RPM changed: {value}");
        }
    }


    public async Task Parse()
    {

        status = ParserStatus.Processing;

        string url, html;
        while (to_check.Any() && (visited.Count() < page_limit) && status == ParserStatus.Processing)
        {
            url = to_check.Pop();
            if (!visited.Contains(url))
            {
                Console.WriteLine($"Скачивание: {url}");
                visited.Add(url);
                Task<string> t = getHTML($"https://{url}");
                await t;
                if (t.IsCompletedSuccessfully)
                {
                    Console.WriteLine("Успешно");

                    html = t.Result;
                    var urls = findAndFormatUrls(html, url);

                    Console.WriteLine($"Ссылок найдено: {urls.Count()}");
                    var links = validateAndSortLinks(urls, root_url, url);
                    Console.WriteLine($"Ссылок после фильтра: {links.Count()}");

										Console.WriteLine(String.Join("\n", links.Where(x => x.Type == LinkType.Domain).Select(x => x.URL)));
										Console.WriteLine("LIIIIINIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIKS");
                    Graph.AddRange(links);

                    var domain_links = links.Where(l => l.Type == LinkType.Domain && l.ContentType == LinkContentType.None).Select(l => l.URL).ToHashSet();
                    foreach (string link in domain_links)
                        to_check.Push(link);

                }
                else
                {
                    new_page_notifier?.Invoke($"Ошибка:{url}");
                    throw t.Exception;
                }
                break;
            }
        }

        DomainTree = Graph
            .Where(l => (l.Type == Parser.LinkType.Domain && l.ContentType == Parser.LinkContentType.None))
            .Select(l => l.URL)
            .ToHashSet();

        DomainImages = Graph
            .Where(l => (l.Type == Parser.LinkType.Domain && l.ContentType == Parser.LinkContentType.Image))
            .Select(l => l.URL)
            .ToHashSet();


        if (status == ParserStatus.Stopping)
            status = ParserStatus.Paused;
        else
            status = ParserStatus.Done;

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



    public void PauseToggle()
    {
        switch (status)
        {
            case ParserStatus.Processing:
                status = ParserStatus.Stopping;
                break;

            case ParserStatus s when
                            s == ParserStatus.Paused || s == ParserStatus.Waiting:
                Parse();
                break;
        }
    }


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


    public enum ParserStatus
    {
        Waiting, Processing, Paused, Error, Stopping, Done
    }

    private async Task<string> getHTML(string url)
    {
        while ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - last_request < request_delay)
            await Task.Delay(10);
        var result = await getHTML_Unsafe(url);
        last_request = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        return result;
    }

    private async Task<string> getHTML_Unsafe(string url)
    {
        new_page_notifier?.Invoke(url);
        try
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                var result = await content.ReadAsStringAsync();
                return result;
            }
        }
        catch (Exception e)
        {
            warning_notifier?.Invoke($"Ошибка получения страницы: {url} {e.StackTrace}");
            return null;
        }
    }




}
