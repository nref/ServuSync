using CommandLine;

namespace ServuSync;

public class Options
{
  [Option("watch", Required = false, HelpText = "Watch continuously for new files. Downloads new files.")]
  public bool Watch { get; set; }

  [Option("list", Required = false, HelpText = "List all files in the given directory, within the given date range")]
  public bool List { get; set; }

  [Option("download", Required = false, HelpText = "Download all files in the given directory, within the given date range")]
  public bool Download { get; set; }

  [Option('d', "directory", Required = false, HelpText = "Directory to scan")]
  public string? Directory { get; set; }

  [Option("after", Required = false, HelpText = "Exclude files before this date")]
  public DateTime After { get; set; }

  [Option("before", Required = false, HelpText = "Exclude files after this date")]
  public DateTime Before { get; set; }
}
