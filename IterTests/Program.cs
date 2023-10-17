using ParserCore;
using LinkProcessor;

public class Programm
{
    public static void Main(string[] args)
    {
			parserTests();
    }



    static void treeBuilderTests()
    {
        TreeBuilder tb = new TreeBuilder();
        tb.loadLinksFromFile("./tree_builder_tests.txt");
        tb.buildTree();

    }


    static void linkFinderTests()
    {
        string url = @"https://ssau.ru";
        string domain_url = @"ssau.ru";
        PageDownloader pd = new PageDownloader();
        pd.requestDelay = 50;
        LinkFinder lf = new LinkFinder();

        Task<string> task = pd.getHTML(url);
        task.Wait();
        string html = task.Result;

        if (html != "")
        {

            var finded_domain_links = LinkFinder.getDomainLinks(html, url, domain_url);
            Console.WriteLine($"domain links: {finded_domain_links.Count()}");
            //Console.WriteLine(String.Join("\n", finded_domain_links));
            Console.WriteLine("Проверка ссылок");
            int error_counter = 0;
            foreach (string link in finded_domain_links)
            {
                var t = pd.getHTML($"https://{link}");
                t.Wait();
                if (t.Result == "")
                {
                    Console.WriteLine($"Ошибка получения страницы: {link}");
                    error_counter++;
                }
                else
                    Console.WriteLine(link);
            }

            Console.WriteLine($"Число ошибок: {error_counter}");

        }
        else
            Console.WriteLine("Ошибка получения страницы!");


    }
    static void parserTests()
    {

        Parser p = new Parser();

        p.URL = "ssau.ru";
        p.RPM = 60;
        p.PageLimit = 5;

        //p.AddNewPageNotifier(Console.WriteLine);
        //p.AddWarningNotifier(Console.WriteLine);

        //Console.WriteLine("Ждем");
        p.StartParsing();
        while (p.Status == Parser.ParserStatus.Processing)
        {
            Thread.Sleep(50);
        }

        Console.WriteLine(String.Join("\n", p.DomainTree));


    }

















}
