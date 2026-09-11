using System.Collections.Concurrent;
using System.Reflection;

namespace CustomJsonSerializer.Serialization;

public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

    public static PropertyInfo[] GetProperties(Type type)
    {
        return Cache.GetOrAdd(
            type,
            t => t.GetProperties( BindingFlags.Public | BindingFlags.Instance)
        );
    }
}