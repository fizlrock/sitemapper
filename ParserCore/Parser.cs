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
        Status = ParserStatus.Waiting;
        root_url = "";
        Graph = new List<Link>();

    }



    private Stack<string> to_check;
    private HashSet<string> visited;
    public List<Link> Graph { private set; get; }
    public HashSet<string> DomainTree { private set; get; }
    public HashSet<string> DomainImages { private set; get; }




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
            if (Status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во посещенных страниц должно быть больше нуля");
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
            if (Status != ParserStatus.Waiting)
                throw new Exception("Редактирование настроек допустимо только в режиме ожидания");
            if (value <= 0)
                throw new ArgumentException("Ограничение на кол-во запросов в минуту должно быть больше нуля");
            pd.requestDelay = 60000 / value;
        }
    }


    private void processHTML(string html, string url)
    {


        var finded_domain_links = LinkFinder.getDomainLinks(html, $"https://{url}", url);
        Console.WriteLine($"domain links: {finded_domain_links.Count()}");
        foreach (string domain_url in finded_domain_links)
        {
            DomainTree.Add(domain_url);
            to_check.Push(domain_url);
        }
    }

    private async Task processNextLink()
    {
        string url, html;
        url = to_check.Pop();

        if (visited.Contains(url))
            return;

        visited.Add(url);
        Console.WriteLine($"https://{url}");
        Task<string> t = pd.getHTML($"https://{url}");
        await t;
        html = t.Result;

        if (html != "")
        {
            processHTML(html, url);
        }
        else
        {
            new_page_notifier?.Invoke($"Ошибка:{url} ");
						Console.WriteLine($"Ошибка {url}");
        }
    }






    public async void StartParsing()
    {
        Status = ParserStatus.Processing;

        while (to_check.Any() && (visited.Count() < page_limit) && Status == ParserStatus.Processing)
        {
            await processNextLink();
            Console.WriteLine($"to_check_size:{to_check.Count()}");

            Console.WriteLine($"tree_size:{DomainTree.Count()}");
            Console.Write("\n\n\n");
						Console.ReadLine();
        }

        if (Status == ParserStatus.Stopping)
            Status = ParserStatus.Paused;
        else
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
