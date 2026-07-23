using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace App;

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
}
