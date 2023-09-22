using System.Net;

namespace MaSync;

public static class CookieMapper
{
  public static System.Net.Cookie MapSystemCookie(this Cookie c) => new()
  {
    Name = c.Name ?? "",
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    Secure = c.IsSecure,
    Expires = c.Expires,
  };

  public static Cookie MapModel(this System.Net.Cookie c) => new()
  {
    Name = c.Name,
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    IsSecure = c.Secure,
    Expires = c.Expires,
  };
  
  public static CookieContainer MapCookieContainer(this Dictionary<string, Cookie> cookies)
  { 
    var cookieContainer = new CookieContainer();
    if (cookies == null) { return cookieContainer; }

    foreach (var cookie in cookies.Values)
    {
      cookieContainer.Add(cookie.MapSystemCookie());
    }

    return cookieContainer;
  }

  public static Dictionary<string, Cookie> MapModel(this CookieContainer cookies) => cookies
    .GetAllCookies()
    .Select(c => c.MapModel())
    .ToDictionaryAllowDuplicateKeys(c => c.Name ?? "", c => c);
}
