using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ServuSync;

public class CompositionRoot
{
  private readonly IContainer container_;

  public CompositionRoot()
  {
    IConfiguration config = BootstrapConfig();

    var builder = new ContainerBuilder();
    builder.RegisterType<ServuClient>().As<IServuClient>().SingleInstance();
    builder.RegisterType<ServuService>().As<IServuService>();
    builder.RegisterType<CookieRepo>().As<ICookieRepo>();

    string user = config.GetValue<string>("username") ?? "";
    string pass = config.GetValue<string>("password") ?? "";
    var maConfig = new ServuConfig(user, pass);
    builder.RegisterInstance(maConfig).As<ServuConfig>();

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