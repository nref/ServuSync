namespace ServuSync;

public interface IServuService
{
  Task WatchAsync(string directory, CancellationToken ct = default);
  Task<List<ServuFile>> ListAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default);
  Task DownloadAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default);
}
