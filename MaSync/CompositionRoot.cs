using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MaSync;

public class CompositionRoot
{
  private readonly IContainer container_;

  public CompositionRoot()
  {
    IConfiguration config = BootstrapConfig();

    var builder = new ContainerBuilder();
    builder.RegisterType<MaClient>().As<IMaClient>().SingleInstance();
    builder.RegisterType<MaService>().As<IMaService>();
    builder.RegisterType<CookieRepo>().As<ICookieRepo>();

    string user = config.GetValue<string>("username") ?? "";
    string pass = config.GetValue<string>("password") ?? "";
    var maConfig = new MaConfig(user, pass);
    builder.RegisterInstance(maConfig).As<MaConfig>();

    ILoggerFactory factory = SetupLogging(config);
    builder.RegisterInstance(factory).As<ILoggerFactory>();
    builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

    container_ = builder.Build();
  }

  private IConfiguration BootstrapConfig()
  {
    IConfiguration config = new ConfigurationBuilder()
     .SetBasePath(AppContext.BaseDirectory) // exe directory
     .AddJsonFile("appsettings.json", false)
     .Build();

    return config;
  }

  public T Get<T>() where T : notnull => container_.Resolve<T>();

  private ILoggerFactory SetupLogging(IConfiguration config)
  {
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(config)
        .Enrich.FromLogContext()
        .CreateLogger();

    return new LoggerFactory().AddSerilog(logger);
  }
}