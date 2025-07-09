using Serilog.Events;

namespace SampleProject.Core.Options;

public sealed class LoggingOptions
{
    public LogEventLevel ConsoleLogLevel { get; init; }
    public SeqLoggingOptions Seq { get; init; }
    public string Version { get; init; }
}

public sealed class SeqLoggingOptions
{
    public bool Enabled { get; init; }
    public string ServerUrl { get; init; }
    public string ApiKey { get; init; }
}