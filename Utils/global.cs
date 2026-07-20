using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace App;

public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
}

public static class G
{
    public static IEnumerable<T> EnumIterate<T>()
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    [DoesNotReturn]
    public static void Todo(string? message = null)
    {
        throw new NotImplementedException($"TODO: {message ?? "no message"}");
    }

    [DoesNotReturn]
    public static T Todo<T>(string? message = null)
    {
        throw new NotImplementedException($"TODO: {message ?? "no message"}");
    }

    [DoesNotReturn]
    public static void Unreachable(string? message = null)
    {
        throw new UnreachableException($"UNREACHABLE: {message ?? "no message"}");
    }

    [DoesNotReturn]
    public static T Unreachable<T>(string? message = null)
    {
        throw new UnreachableException($"UNREACHABLE: {message ?? "no message"}");
    }

    public static void Log(LogLevel level, string message)
    {
        var writer = level switch {
            LogLevel.Debug or LogLevel.Info or LogLevel.Warning => Console.Out,
            LogLevel.Error or LogLevel.Fatal => Console.Error,
            _ => G.Unreachable<TextWriter>(nameof(LogLevel)),
        };
        var level_str = level switch {
            LogLevel.Debug   => "DEBUG",
            LogLevel.Info    => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error   => "ERROR",
            LogLevel.Fatal   => "FATAL",
            _ => G.Unreachable<string>("LogLevel"),
        };
        writer.WriteLine($"{level_str}: {message}");
    }
}
