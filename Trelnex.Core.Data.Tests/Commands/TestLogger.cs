using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Data.Tests.Commands;

internal class TestLogger : ILogger
{
    public List<TestLogEntry> LogEntries { get; } = [];

    public IDisposable? BeginScope<TState>(
        TState state) where TState : notnull
        => null;

    public bool IsEnabled(
        LogLevel logLevel)
        => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new()
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception
        });
    }
}

internal class TestLogEntry
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
