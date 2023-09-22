using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MaSync;

public class CookieRepo : ICookieRepo
{
  private const string cookiesJson_ = "cookies.json";
  private readonly ILogger<CookieRepo> log_;

  public CookieRepo(ILogger<CookieRepo> log)
  {
    log_ = log;
  }

  public async Task SaveCookies(Dictionary<string, Cookie>? cookies)
  {
    try
    {
      string json = JsonSerializer.Serialize(cookies);
      await File.WriteAllTextAsync(cookiesJson_, json);
    }
    catch (Exception e)
    {
      log_.LogError($"Could not save cookies: {e.Message}");
    }
  }

  public async Task<Dictionary<string, Cookie>?> ReadCookies()
  {
    try
    {
      string json = await File.ReadAllTextAsync(cookiesJson_);
      var cookies = JsonSerializer.Deserialize<Dictionary<string, Cookie>?>(json);
      return cookies;
    }
    catch (Exception e)
    {
      log_.LogError($"Could not load cookies: {e.Message}");
      return null;
    }
  }
}