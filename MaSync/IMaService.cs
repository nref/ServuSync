namespace MaSync;

public interface IMaService
{
  Task WatchAsync(string directory, CancellationToken ct = default);
  Task<List<MaFile>> ListAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default);
  Task DownloadAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default);
}
