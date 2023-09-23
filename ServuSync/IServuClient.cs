namespace ServuSync;

public interface IServuClient
{
  Dictionary<string, Cookie>? Cookies { get; set; }

  Task<bool> PingAsync();
  Task<bool> LoginAsync();
  Task<List<ServuFile>> ListAsync(string dir, CancellationToken ct = default);
  Task<bool> DownloadAsync(string file, CancellationToken ct = default);
}