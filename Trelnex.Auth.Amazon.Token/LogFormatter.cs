using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Trelnex.Auth.Amazon.Token;

public class LogFormatter() : ConsoleFormatter(nameof(LogFormatter))
{
    private static readonly Type? _textWriterExtensionsType =
        typeof(ConsoleFormatter).Assembly
        .GetType("Microsoft.Extensions.Logging.Console.TextWriterExtensions");

    private static readonly MethodInfo? _writeColoredMessageMethod =
        _textWriterExtensionsType?
        .GetMethod("WriteColoredMessage", BindingFlags.Public | BindingFlags.Static);

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(
            logEntry.State,
            logEntry.Exception);

        if (string.IsNullOrEmpty(message))
        {
            textWriter.WriteLine();
            return;
        }

        var logLevel = logEntry.LogLevel switch
        {
            LogLevel.Critical => "[c]",
            LogLevel.Debug => "[d]",
            LogLevel.Error => "[e]",
            LogLevel.Information => "[i]",
            LogLevel.Trace => "[t]",
            LogLevel.Warning => "[w]",
            _ => "[?]"
        };

        var consoleColors = logEntry.LogLevel switch
        {
            LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColors(null, null)
        };

        _writeColoredMessageMethod?.Invoke(
            null,
            [textWriter, logLevel, consoleColors.Background, consoleColors.Foreground]);

        textWriter.Write(": ");
        textWriter.WriteLine(message);
    }

    private readonly struct ConsoleColors(
        ConsoleColor? foreground,
        ConsoleColor? background)
    {
        public ConsoleColor? Foreground => foreground;
        public ConsoleColor? Background => background;
    }
}
