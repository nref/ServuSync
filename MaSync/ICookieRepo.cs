namespace MaSync;

public interface ICookieRepo
{
  Task<Dictionary<string, Cookie>?> ReadCookies();
  Task SaveCookies(Dictionary<string, Cookie>? cookies);
}