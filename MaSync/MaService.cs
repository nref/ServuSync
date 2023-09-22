using Autofac;
using Microsoft.Extensions.Logging;

namespace MaSync;

public class MaService : IMaService
{
  private readonly ILogger<MaService> log_;
  private readonly IMaClient client_;

  public MaService(ILogger<MaService> log, IMaClient client)
  {
    log_ = log;
    client_ = client;
  }

  public async Task WatchAsync(string directory, CancellationToken ct = default)
  {
    DateTime after = DateTime.UtcNow;

    await Task.Run(async () =>
    {
      log_.LogInformation("Watching directory {directory} for new files...", directory);

      while (!ct.IsCancellationRequested)
      {
        List<MaFile> files = await ListAsync(directory, after, DateTime.MaxValue);
        after = DateTime.UtcNow;

        await DownloadInParallelAsync(directory, files, ct);
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
      }
    }, ct);
  }

  public async Task<List<MaFile>> ListAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default)
  {
    List<MaFile> files = await client_.ListAsync(directory, ct);
    List<MaFile> inRange = files.Where(f => f.Date > after && f.Date < before).ToList();
    
    foreach (MaFile file in inRange)
    {
      log_.LogInformation($"{file.Date} {file.Size}\t \"{file.Name}\"");
    }

    return inRange;
  }

  public async Task DownloadAsync(string directory, DateTime after, DateTime before, CancellationToken ct = default)
  {
    List<MaFile> files = await client_.ListAsync(directory);
    List<MaFile> inRange = files.Where(f => f.Date > after && f.Date < before).ToList();

    log_.LogInformation("Downloading {count} files", inRange.Count);
    await DownloadInParallelAsync(directory, inRange, ct);
  }

  private async Task DownloadInParallelAsync(string directory, List<MaFile> inRange, CancellationToken ct = default)
  {
    await Parallel.ForEachAsync(inRange, ct, async (file, ct) =>
    {
      string path = $"{directory}/{file.Name}";

      log_.LogInformation("Downloading {path}", path);

      bool ok = await client_.DownloadAsync(path, ct);

      if (!ok)
      {
        log_.LogError("Could not download {path}", path);
      }
    });
  }
}