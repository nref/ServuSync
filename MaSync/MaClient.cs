using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System;

namespace MaSync;

public partial class MaClient : IMaClient
{
  [GeneratedRegex(".*code=(\\w+)")]
  private static partial Regex GetCodeRegex();

  private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/117.0";
  private const string DOMAIN = "yourcompany.com";

  private CookieContainer cookies_ = new();
  private readonly ICookieRepo cookieRepo_;
  private readonly ILogger<MaClient> log_;

  public Dictionary<string, Cookie>? Cookies { get; set; }
  public MaConfig Config { get; }

  public MaClient(ICookieRepo cookieRepo, MaConfig config, ILogger<MaClient> log)
  {
    cookieRepo_ = cookieRepo;
    Config = config;
    log_ = log;

    Cookies = cookieRepo.ReadCookies().GetAwaiter().GetResult();
  }

  public async Task<bool> LoginAsync()
  {
    using HttpClient client = GetClient(withCookies: false);

    log_.LogInformation("Loading file share login page...");
    HttpResponseMessage resp = await client.GetAsync($"https://file.{DOMAIN}");

    // should get 302 redirect to authentication.{DOMAIN}
    if (resp.StatusCode != HttpStatusCode.Redirect || resp.Headers.Location?.Host != $"authentication.{DOMAIN}")
    {
      log_.LogError("Error: Did not get redirected to NetScaler gateway");
      return false;
    }

    log_.LogInformation("Loading NetScaler gateway login page...");
    HttpResponseMessage resp2 = await client.GetAsync(resp.Headers.Location);

    var netScalerCreds = new FormUrlEncodedContent(new[]
    {
      new KeyValuePair<string, string>("login", Config.Username),
      new KeyValuePair<string, string>("passwd", Config.Password),
    });

    log_.LogInformation("Logging in to NetScaler gateway...");
    HttpResponseMessage resp3 = await client.PostAsync($"https://authentication.{DOMAIN}/cgi/login", netScalerCreds);
    string? resp3Content = await resp3.Content.ReadAsStringAsync();

    if (resp3.StatusCode != HttpStatusCode.OK)
    {
      log_.LogError("Error: Could not login to NetScaler gateway. Check password.");
      if (!string.IsNullOrEmpty(resp3Content))
      {
        log_.LogError("More info:");
        log_.LogError($"HTTP status code: {resp3.StatusCode}");
        log_.LogError($"HTTP response content: {resp3.StatusCode}");
        log_.LogError(resp3Content);
      }
      return false;
    }

    Regex regex = GetCodeRegex();
    Match match = regex.Match(resp3Content);
    if (!match.Success || match.Groups.Count < 2)
    {
      log_.LogError("Could not get NetScaler authentication token");
      return false;
    }

    string code = match.Groups[1].Value;

    log_.LogInformation("Going back to file share auth path...");
    HttpResponseMessage resp4 = await client.GetAsync($"https://file.{DOMAIN}/cgi/selfauth?code={code}");

    var ftpCreds = new FormUrlEncodedContent(new[]
    {
      new KeyValuePair<string, string>("user", Config.Username),
      new KeyValuePair<string, string>("pword", Config.Password),
      new KeyValuePair<string, string>("viewshare", ""),
      new KeyValuePair<string, string>("language", "en-US"),
    });

    log_.LogInformation("Logging in to file share...");
    long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    HttpResponseMessage resp5 = await client.PostAsync($"https://file.{DOMAIN}/Login.xml?Command=Login&Sync={time}", ftpCreds);

    if (resp5.StatusCode != HttpStatusCode.OK)
    {
      log_.LogError("Could not login to file share with the given credentials.");
      return false;
    }

    if (!ValidateCookie($"https://file.{DOMAIN}", "Session"))
    {
      return false;
    }

    Cookies = cookies_.MapModel();
    await cookieRepo_.SaveCookies(Cookies);

    log_.LogInformation("Login OK");
    return true;
  }

  public async Task<bool> PingAsync()
  {
    using HttpClient client = GetClient();
    long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    HttpResponseMessage resp = await client.PostAsync($"https://file.{DOMAIN}/?Command=NOOP&Sync={time}", null);

    if (resp.StatusCode != HttpStatusCode.OK)
    {
      log_.LogError("Could not ping file share");
      return false;
    }

    return true;
  }

  public async Task<List<MaFile>> ListAsync(string dir, CancellationToken ct = default)
  {
    using HttpClient client = GetClient();
    long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    HttpResponseMessage resp = await client.GetAsync($"https://file.{DOMAIN}/Web%20Client/ListError.xml?Command=List&Dir={dir}&sync={time}", ct);

    if (resp.StatusCode != HttpStatusCode.OK)
    {
      log_.LogError($"Could not list directory {dir}");
      return new List<MaFile>();
    }

    string xml = await resp.Content.ReadAsStringAsync(ct);
    //log_.LogDebug(xml);

    var doc = XDocument.Parse(xml);

    List<MaFile> files = doc
      .Descendants("files")
      .Elements("file")
      .Select(e =>
      {
        if (!long.TryParse(e.Element("FileSize")?.Value, out long fileSize))
        {
          fileSize = 0;
        }

        if (!long.TryParse(e.Element("FileDate")?.Value, out long fileDateMs))
        {
          fileDateMs = 0;
        }

        string name = e.Element("FileName")?.Value ?? "";
        if (name is not null)
        {
          name = WebUtility.UrlDecode(name);
        }

        return new MaFile
        {
          Name = name ?? "(Untitled)",
          Size = fileSize,
          Date = DateTimeOffset.FromUnixTimeSeconds(fileDateMs).DateTime
        };
      }).ToList();

    return files;
  }

  public async Task<bool> DownloadAsync(string file, CancellationToken ct = default)
  {
    using HttpClient client = GetClient();
    HttpResponseMessage resp = await client.GetAsync($"https://file.{DOMAIN}/Web%20Client/?Command=Download&File={file}", ct);

    if (resp.StatusCode != HttpStatusCode.OK)
    {
      log_.LogError("Server returned error requesting file {file}", file);
      return false;
    }

    const int bufSize = 128 * 1024; // 128 kB
    var buffer = new byte[bufSize];

    try
    {
      Directory.CreateDirectory("downloads");
      using var fs = new FileStream($"downloads/{Path.GetFileName(file)}", FileMode.Create);

      await SaveToFile(resp.Content, buffer, fs, ct);
      return true;
    }
    catch (Exception e)
    {
      log_.LogError("Exception downloading file {file}: {exception}", file, e.Message);
      return false;
    }
  }

  private static async Task SaveToFile(HttpContent content, byte[] buffer, FileStream fs, CancellationToken ct = default)
  {
    using var stream = await content.ReadAsStreamAsync();
    long totalBytes = content.Headers.ContentLength ?? 0;
    long bytesRead = 0;

    int count;
    while (!ct.IsCancellationRequested && (count = await stream.ReadAsync(buffer, ct)) != 0)
    {
      bytesRead += count;
      await fs.WriteAsync(buffer.AsMemory(0, count), ct);
    }
  }

  private bool ValidateCookie(string uri, string name)
  {
    List<System.Net.Cookie> cookies = cookies_
      .GetCookies(new Uri(uri))
      .Cast<System.Net.Cookie>()
      .ToList();

    System.Net.Cookie? cookie = cookies.Find(e => e.Name == name);
    string value = cookie?.Value ?? "";

    if (string.IsNullOrEmpty(value))
    {
      log_.LogError($"Error: Expected to find cookie '{name}' for '{uri}'");
      return false;
    }

    return true;
  }

  private HttpClient GetClient(bool withCookies = true)
  {
    cookies_ = withCookies ? GetCachedCookies() : new();

    var handler = new SocketsHttpHandler
    {
      AllowAutoRedirect = false,
      UseCookies = true,
      CookieContainer = cookies_,
    };

    var client = new HttpClient(handler);

    client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
    client.DefaultRequestHeaders.ConnectionClose = false;

    return client;
  }

  private CookieContainer GetCachedCookies() => Cookies?.MapCookieContainer() ?? new();
}