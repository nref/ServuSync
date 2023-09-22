using CommandLine;
using Microsoft.Extensions.Logging;

namespace MaSync;

public class Program
{
  public static async Task Main(string[] args)
  {
    var options = Parser.Default.ParseArguments<Options>(args);

    var root = new CompositionRoot();
    var client = root.Get<IMaClient>();
    var service = root.Get<IMaService>();
    var log = root.Get<ILogger<Program>>();

    if (!await client.PingAsync())
    {
      bool loginOk = await client.LoginAsync();
      if (!loginOk) { return; }
    }

    string? directory = null;

    if (options.Value.List || options.Value.Download || options.Value.Watch)
    {
      directory = options.Value.Directory;
      if (directory is null)
      {
        log.LogError("Please provide a directory with the --directory flag");
        return;
      }
    }

    if (directory != null && (options.Value.List || options.Value.Download))
    { 
      DateTime after = options.Value.After == default ? DateTime.MinValue : options.Value.After;
      DateTime before = options.Value.Before == default ? DateTime.MaxValue : options.Value.Before;

      if (options.Value.List)
      {
        await service.ListAsync(directory, after, before);
      }

      if (options.Value.Download)
      {
        await service.DownloadAsync(directory, after, before);
      }
    }

    if (directory != null && options.Value.Watch) 
    {
      await service.WatchAsync(directory);
    }
  }
}