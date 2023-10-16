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

    private static Regex validate_url_pattern = new Regex(@"^(?'protocol'https?:\/\/)?(?'domain'[a-zA-Z.0-9-_]+)(\/[a-zA-Z0-9-_?]+)*(?'filetype'\.\S{1,4})?\/?$");
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
 
                }
                else
                {
                    new_page_notifier?.Invoke($"Ошибка:{url}");
                    throw t.Exception;
                }
                break;
            }
        }


        if (status == ParserStatus.Stopping)
            status = ParserStatus.Paused;
        else
            status = ParserStatus.Done;

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
