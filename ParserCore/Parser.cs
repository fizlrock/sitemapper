using System.Text.RegularExpressions;

namespace ParserCore;
public class Parser
{
    public Parser()
    {
        pd = new PageDownloader();
        Reset();
        PageLimit = 100;
        RPM = 60;

    }

    private PageDownloader pd;
    private static Regex validate_url_pattern = new Regex(@"^(?'protocol'https?:\/\/)?(?'domain'[a-zA-Z.0-9-_]+)(\/[a-zA-Z0-9-_?]+)*(?'filetype'\.\S{1,4})?\/?$");
    private ParserStatus _status;
    public ParserStatus Status
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
        ExternalImages = new HashSet<string>();
        Status = ParserStatus.Waiting;
        root_url = "";
        Graph = new List<Link>();

    }



    public Stack<string> to_check { private set; get; }
    public HashSet<string> visited { private set; get; }
    public List<Link> Graph { private set; get; }
    public HashSet<string> DomainTree { private set; get; }
    public HashSet<string> DomainImages { private set; get; }
    public HashSet<string> ExternalImages { private set; get; }




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
            if (Status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            var m = validate_url_pattern.Match(value);
            if ((!m.Success || Status == ParserStatus.Paused))
                throw new ArgumentException("Переданная строка не является URL");
            Reset();
            root_url = value;
            to_check.Push(root_url);
        }
        get => root_url;
    }





    public int PageLimit
    {
        get => page_limit;
        set
        {
            if (Status == ParserStatus.Processing)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во посещенных страниц должно быть больше нуля");
            Console.WriteLine($"page_limit_changed: {value}");
            page_limit = value;
        }
    }

    private int page_limit;
    public int RPM
    {
        get
        {
            return (int)(1 / ((double)pd.requestDelay / 60000));
        }
        set
        {
            if (Status == ParserStatus.Processing)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во запросов в минуту должно быть больше нуля");
            Console.WriteLine($"RPM changed:{value}");
            pd.requestDelay = 60000 / value;
        }
    }


    private void processHTML(string html, string url)
    {

        var links = LinkFinder.findAndFormatUrls(html, root_url);
        var sorted_links = LinkFinder.sortLinks(links, root_url);

        var domain_urls = sorted_links
                            .Where(l => l.Type == LinkType.Domain)
                            .Where(l => l.ContentType == LinkContentType.None)
                            .Select(l => l.URL);
        DomainTree.UnionWith(domain_urls);

        var domain_images = sorted_links
                    .Where(l => l.Type == LinkType.Domain)
                    .Where(l => l.ContentType == LinkContentType.Image)
                    .Select(l => l.URL);
        DomainImages.UnionWith(domain_images);

        var ext_images = sorted_links
                                    .Where(l => l.Type == LinkType.External)
                                    .Where(l => l.ContentType == LinkContentType.Image)
                                    .Select(l => l.URL);
        ExternalImages.UnionWith(ext_images);

        foreach (string link in domain_urls)
            to_check.Push(link);

    }

    private async Task processNextLink()
    {
        string url, html = "";
        url = to_check.Pop();

        if (visited.Contains(url))
            return;

        visited.Add(url);

        new_page_notifier?.Invoke(url);

        int attempt_counter = 0;
        do
        {
            var t = pd.getHTML($"https://{url}");
            await t;
            if (t.IsCompletedSuccessfully)
                html = t.Result;
            attempt_counter++;
        }
        while (attempt_counter < 2 && html == "");

        if (attempt_counter > 2 && html == "")
        {
            new_page_notifier?.Invoke($"Ошибка:{url} ");
            Console.WriteLine($"Ошибка {url}");
            return;
        }
        else
            processHTML(html, url);
    }





    public async Task StartParsing()
    {
        Status = ParserStatus.Processing;

        while (to_check.Any() && (visited.Count() < page_limit) && Status == ParserStatus.Processing)
        {
            Task t = processNextLink();
            await t;
            if (t.Exception != null)
                Console.WriteLine(t.Exception.StackTrace);
        }

        Status = ParserStatus.Paused;
        if (!to_check.Any())
            Status = ParserStatus.Done;
    }


    public void PauseToggle()
    {
        switch (Status)
        {
            case ParserStatus.Processing:
                Status = ParserStatus.Stopping;
                break;

            case ParserStatus s when
                            s == ParserStatus.Paused || s == ParserStatus.Waiting:
                StartParsing();
                break;
        }
    }


    public enum ParserStatus
    {
        Waiting, Processing, Paused, Error, Stopping, Done
    }






}
