namespace App.Helpers;

public static class Log
{
    public enum Level {
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
    }

    public static void Write(Level level, string message)
    {
        var writer = level switch {
            Level.Debug or Level.Info or Level.Warning => Console.Out,
            Level.Error or Level.Fatal => Console.Error,
            _ => G.Unreachable<TextWriter>(nameof(Level)),
        };
        var level_str = level switch {
            Level.Debug   => "DEBUG",
            Level.Info    => "INFO",
            Level.Warning => "WARNING",
            Level.Error   => "ERROR",
            Level.Fatal   => "FATAL",
            _ => G.Unreachable<string>(nameof(Level)),
        };
        writer.WriteLine($"{level_str}: {message}");
    }

    public static void Debug(string message)   => Write(Level.Debug, message);
    public static void Info(string message)    => Write(Level.Info, message);
    public static void Warning(string message) => Write(Level.Warning, message);
    public static void Error(string message)   => Write(Level.Error, message);
    public static void Fatal(string message)   => Write(Level.Fatal, message);
}

public struct ViewMessage
{
    public enum Type {
        None,
        Error,
        Good,
    }

    public static string error_css_class = "form-error";
    public static string good_css_class = "form-good";

    public Type type;
    public string message;

    public string GetClass()
    {
        return type switch {
            Type.None  => "",
            Type.Error => error_css_class,
            Type.Good  => good_css_class,
            _ => G.Unreachable<string>(nameof(GetClass)),
        };
    }

    public static ViewMessage Error(string message)
    {
        return new ViewMessage {
            type = Type.Error,
            message = message,
        };
    }

    public static ViewMessage Good(string message)
    {
        return new ViewMessage {
            type = Type.Good,
            message = message,
        };
    }

    public static ViewMessage None()
    {
        return new ViewMessage {
            type = Type.None,
        };
    }

    public static List<ViewMessage> FromStrings(Type type, IEnumerable<string> strings)
    {
        var messages = new List<ViewMessage>();
        foreach (var str in strings) {
            messages.Add(new ViewMessage {
                type = type,
                message = str,
            });
        }
        return messages;
    }

    public static List<ViewMessage> FromStringsError(IEnumerable<string> strings)
    {
        return FromStrings(Type.Error, strings);
    }

    public static List<ViewMessage> FromStringsGood(IEnumerable<string> strings)
    {
        return FromStrings(Type.Good, strings);
    }

    public static List<ViewMessage> MapCollection<T>(Type type, IEnumerable<T> collection, Func<T, string> mapper_func)
    {
        var messages = new List<ViewMessage>();
        foreach (var it in collection) {
            messages.Add(new ViewMessage {
                type = type,
                message = mapper_func(it),
            });
        }
        return messages;
    }

    public static List<ViewMessage> MapCollectionError<T>(IEnumerable<T> collection, Func<T, string> mapper_func)
    {
        return MapCollection(Type.Error, collection, mapper_func);
    }

    public static List<ViewMessage> MapCollectionGood<T>(IEnumerable<T> collection, Func<T, string> mapper_func)
    {
        return MapCollection(Type.Good, collection, mapper_func);
    }
}

public class ViewPath
{
    public string path_prefix = "";
    public string file_extention = ".cshtml";

    public string GetPath(string name)
    {
        return Path.GetFullPath($"/{path_prefix}/{name}{file_extention}");
    }
}
