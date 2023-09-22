namespace MaSync;

public class Cookie
{
  public string? Name { get; set; }
  public string? Value { get; set; }
  public string? Domain { get; set; }
  public string? Path { get; set; }
  public bool HttpOnly { get; set; }
  public bool IsSecure { get; set; }
  public DateTime Expires { get; set; }
}
