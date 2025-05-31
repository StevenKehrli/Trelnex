using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Trelnex.Auth.Amazon.Admin;

/// <summary>
/// A custom console formatter that provides colored log output with abbreviated log level prefixes.
/// </summary>
public class LogFormatter() : ConsoleFormatter(nameof(LogFormatter))
{
    /// <summary>
    /// The type information for the internal TextWriterExtensions class used for colored console output.
    /// </summary>
    private static readonly Type? _textWriterExtensionsType =
        typeof(ConsoleFormatter).Assembly
        .GetType("Microsoft.Extensions.Logging.Console.TextWriterExtensions");

    /// <summary>
    /// The method information for the WriteColoredMessage method used to write colored text to the console.
    /// </summary>
    private static readonly MethodInfo? _writeColoredMessageMethod =
        _textWriterExtensionsType?
        .GetMethod("WriteColoredMessage", BindingFlags.Public | BindingFlags.Static);

    /// <summary>
    /// Writes a log entry to the specified text writer with color formatting and abbreviated log level prefixes.
    /// </summary>
    /// <typeparam name="TState">The type of the log entry state.</typeparam>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="scopeProvider">The external scope provider (not used in this implementation).</param>
    /// <param name="textWriter">The text writer to write the log entry to.</param>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        // Extract the formatted message from the log entry using the provided formatter
        string? message = logEntry.Formatter?.Invoke(
            logEntry.State,
            logEntry.Exception);

        // If no message content exists, just write an empty line and return early
        if (string.IsNullOrEmpty(message))
        {
            textWriter.WriteLine();
            return;
        }

        // Map log levels to short, bracketed abbreviations for compact display
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

        // Define color schemes for each log level to provide visual distinction
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

        // Use reflection to call the internal WriteColoredMessage method for colored output
        _writeColoredMessageMethod?.Invoke(
            null,
            [textWriter, logLevel, consoleColors.Background, consoleColors.Foreground]);

        // Write the separator and the actual log message
        textWriter.Write(": ");
        textWriter.WriteLine(message);
    }

    /// <summary>
    /// Represents the foreground and background console colors for log output.
    /// </summary>
    /// <param name="foreground">The foreground console color.</param>
    /// <param name="background">The background console color.</param>
    private readonly struct ConsoleColors(
        ConsoleColor? foreground,
        ConsoleColor? background)
    {
        /// <summary>
        /// Gets the foreground console color.
        /// </summary>
        public ConsoleColor? Foreground => foreground;
        
        /// <summary>
        /// Gets the background console color.
        /// </summary>
        public ConsoleColor? Background => background;
    }
}
