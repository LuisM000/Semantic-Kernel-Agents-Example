using System.ServiceModel.Syndication;
using System.Xml;
using System.ComponentModel;
using Microsoft.SemanticKernel;

/// <summary>
/// A plugin to interact with a blog.
/// </summary>
sealed class BlogInfoPlugin
{
    private const string RssFeedUrl = "https://luisnet.azurewebsites.net/feed";
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlogInfoPlugin"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory to create HTTP clients.</param>
    public BlogInfoPlugin(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Retrieves the latest blog entries.
    /// </summary>
    /// <returns>A list of the latest blog entries.</returns>
    [KernelFunction("get_latest_entries")]
    [Description("Retrieves the latest blog entries.")]
    public async Task<IList<string>> GetLatestEntriesAsync()
    {
        var latestEntries = new List<string>();
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetStringAsync(RssFeedUrl);
        using var stringReader = new StringReader(response);
        using var xmlReader = XmlReader.Create(stringReader);
        var feed = SyndicationFeed.Load(xmlReader);
        if (feed == null)
        {
            return [];
        }

        return feed.Items
            .Select(item =>
            {
                var encodedExtension = item.ElementExtensions.FirstOrDefault(ext =>
                    ext.OuterName == "encoded" &&
                    ext.OuterNamespace == "http://purl.org/rss/1.0/modules/content/");
                
                return encodedExtension?.GetObject<XmlElement>().InnerText ?? string.Empty;
            })
            .Where(entry => !string.IsNullOrEmpty(entry))
            .ToList();
    }
}
