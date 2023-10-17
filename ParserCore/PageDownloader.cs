
namespace ParserCore;


public class PageDownloader
{


    private HttpClient client;

    public PageDownloader()
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2); ;
        lastRequest = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private long lastRequest;
    public long requestDelay;





    public async Task<string> getHTML(string url)
    {
        while ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastRequest < requestDelay)
            await Task.Delay(10);
        var result = await getHTML_Unsafe(url);
        lastRequest = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        return result;
    }

    private async Task<string> getHTML_Unsafe(string url)
    {
        using (HttpResponseMessage response = await client.GetAsync(url))
        using (HttpContent content = response.Content)
        {
            var result = await content.ReadAsStringAsync();
						if(response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
							result = "";
            return result;
        }

    }
}
