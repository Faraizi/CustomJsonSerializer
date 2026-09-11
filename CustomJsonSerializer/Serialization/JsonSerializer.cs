using CustomJsonSerializer.Conversion;
using CustomJsonSerializer.Exceptions;
using CustomJsonSerializer.Parsing;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CustomJsonSerializer.Serialization;

public static class JsonSerializer
{
    public static string Serialize(object? value)
    {
        StringBuilder builder = new();

        HashSet<object> visited =
            new(ReferenceEqualityComparer.Instance);

        SerializeValue(value, builder, visited);

        return builder.ToString();
    }

    public static T? Deserialize<T>(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(
                nameof(json));
        }

        JsonParser parser = new(json);

        object? parsedValue = parser.Parse();

        object? convertedValue =
            JsonConverter.ConvertToType(
                parsedValue,
                typeof(T));

        return (T?)convertedValue;
    }

    private static void SerializeValue(
        object? value,
        StringBuilder builder,
        HashSet<object> visited)
    {
        if (value == null)
        {
            builder.Append("null");
            return;
        }

        Type type = value.GetType();

        if (SerializePrimitive(value, type, builder))
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            SerializeDictionary(
                dictionary,
                builder,
                visited);

            return;
        }

        if (value is IEnumerable enumerable &&
            value is not string)
        {
            SerializeCollection(
                enumerable,
                builder,
                visited);

            return;
        }

        SerializeObject(
            value,
            builder,
            visited);
    }

    private static bool SerializePrimitive(
        object value,
        Type type,
        StringBuilder builder)
    {
        if (value is string text)
        {
            SerializeString(text, builder);
            return true;
        }

        if (value is char character)
        {
            SerializeString(character.ToString(), builder);
            return true;
        }

        if (value is bool boolean)
        {
            builder.Append(
                boolean ? "true" : "false");

            return true;
        }

        if (value is int ||
            value is long ||
            value is float ||
            value is double ||
            value is decimal)
        {
            builder.Append(
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture));

            return true;
        }

        if (value is DateTime dateTime)
        {
            SerializeString(
                dateTime.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                builder);

            return true;
        }

        if (value is Guid guid)
        {
            SerializeString(
                guid.ToString(),
                builder);

            return true;
        }

        if (type.IsEnum)
        {
            SerializeString(
                value.ToString()!,
                builder);

            return true;
        }

        return false;
    }

    private static void SerializeObject(
        object value,
        StringBuilder builder,
        HashSet<object> visited)
    {
        if (!visited.Add(value))
        {
            throw new JsonSerializationException(
                $"Circular reference detected while serializing type '{value.GetType().Name}'.");
        }

        try
        {
            builder.Append('{');

            PropertyInfo[] properties =
                ReflectionCache.GetProperties(
                    value.GetType());

            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                PropertyInfo property = properties[i];

                SerializeString(
                    property.Name,
                    builder);

                builder.Append(':');

                object? propertyValue =
                    property.GetValue(value);

                SerializeValue(
                    propertyValue,
                    builder,
                    visited);
            }

            builder.Append('}');
        }
        finally
        {
            visited.Remove(value);
        }
    }

    private static void SerializeCollection(
        IEnumerable collection,
        StringBuilder builder,
        HashSet<object> visited)
    {
        if (!visited.Add(collection))
        {
            throw new JsonSerializationException(
                $"Circular reference detected while serializing collection '{collection.GetType().Name}'.");
        }

        try
        {
            builder.Append('[');

            bool first = true;

            foreach (object? item in collection)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;

                SerializeValue(
                    item,
                    builder,
                    visited);
            }

            builder.Append(']');
        }
        finally
        {
            visited.Remove(collection);
        }
    }

    private static void SerializeDictionary(
        IDictionary dictionary,
        StringBuilder builder,
        HashSet<object> visited)
    {
        if (!visited.Add(dictionary))
        {
            throw new JsonSerializationException(
                $"Circular reference detected while serializing dictionary '{dictionary.GetType().Name}'.");
        }

        try
        {
            builder.Append('{');

            bool first = true;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw new JsonSerializationException(
                        "Dictionary keys must be strings.");
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;

                SerializeString(
                    key,
                    builder);

                builder.Append(':');

                SerializeValue(
                    entry.Value,
                    builder,
                    visited);
            }

            builder.Append('}');
        }
        finally
        {
            visited.Remove(dictionary);
        }
    }

    private static void SerializeString(
        string value,
        StringBuilder builder)
    {
        builder.Append('"');

        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                default:
                    builder.Append(character);
                    break;
            }
        }

        builder.Append('"');
    }

    public static object? Parse(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(
                nameof(json));
        }

        JsonParser parser =
            new(json);

        return parser.Parse();
    }
}