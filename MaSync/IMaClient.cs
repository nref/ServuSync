namespace MaSync;

public interface IMaClient
{
  Dictionary<string, Cookie>? Cookies { get; set; }

  Task<bool> PingAsync();
  Task<bool> LoginAsync();
  Task<List<MaFile>> ListAsync(string dir, CancellationToken ct = default);
  Task<bool> DownloadAsync(string file, CancellationToken ct = default);
}