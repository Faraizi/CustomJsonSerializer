using System.Collections;
using System.Globalization;
using System.Reflection;
using CustomJsonSerializer.Exceptions;

namespace CustomJsonSerializer.Conversion;

internal static class JsonConverter
{
    public static object? ConvertToType(
        object? value,
        Type targetType)
    {
        if (value == null)
        {
            if (IsNullable(targetType))
            {
                return null;
            }

            throw new JsonSerializationException(
                $"Cannot assign null to non-nullable type '{targetType.Name}'.");
        }

        Type? underlyingType =
            Nullable.GetUnderlyingType(targetType);

        if (underlyingType != null)
        {
            return ConvertToType(
                value,
                underlyingType);
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(string))
        {
            if (value is string text)
            {
                return text;
            }

            throw new JsonSerializationException(
                $"Expected a string but received '{value.GetType().Name}'.");
        }

        if (targetType == typeof(bool))
        {
            if (value is bool)
            {
                return value;
            }

            throw new JsonSerializationException(
                $"Cannot convert '{value}' to bool.");
        }

        if (targetType == typeof(int) ||
            targetType == typeof(long) ||
            targetType == typeof(float) ||
            targetType == typeof(double) ||
            targetType == typeof(decimal))
        {
            try
            {
                return Convert.ChangeType(
                    value,
                    targetType,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new JsonSerializationException(
                    $"Cannot convert '{value}' to '{targetType.Name}'.");
            }
        }

        if (targetType.IsEnum)
        {
            if (value is not string enumText)
            {
                throw new JsonSerializationException(
                    $"Expected a string for enum '{targetType.Name}'.");
            }

            try
            {
                return Enum.Parse(
                    targetType,
                    enumText);
            }
            catch
            {
                throw new JsonSerializationException(
                    $"Invalid value '{enumText}' for enum '{targetType.Name}'.");
            }
        }

        if (targetType == typeof(Guid))
        {
            if (value is not string guidText ||
                !Guid.TryParse(guidText, out Guid guid))
            {
                throw new JsonSerializationException(
                    $"Invalid Guid value '{value}'.");
            }

            return guid;
        }

        if (targetType == typeof(DateTime))
        {
            if (value is not string dateText ||
                !DateTime.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime date))
            {
                throw new JsonSerializationException(
                    $"Invalid DateTime value '{value}'.");
            }

            return date;
        }

        if (value is List<object?> list)
        {
            return ConvertCollection(list, targetType);
        }

        if (value is Dictionary<string, object?> dictionary)
        {
            if (IsDictionaryType(targetType))
            {
                return ConvertDictionary(
                    dictionary,
                    targetType);
            }

            return ConvertObject(
                dictionary,
                targetType);
        }

        throw new JsonSerializationException(
            $"Cannot convert JSON value of type '{value.GetType().Name}' to '{targetType.Name}'.");
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType ||
               Nullable.GetUnderlyingType(type) != null;
    }

    private static object ConvertObject(
        Dictionary<string, object?> dictionary,
        Type targetType)
    {
        object instance;

        try
        {
            instance =
                Activator.CreateInstance(targetType)
                ?? throw new JsonSerializationException(
                    $"Could not create instance of '{targetType.Name}'.");
        }
        catch (MissingMethodException)
        {
            throw new JsonSerializationException(
                $"Type '{targetType.Name}' must have a parameterless constructor.");
        }

        PropertyInfo[] properties =
            targetType.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            if (!dictionary.TryGetValue(
                property.Name,
                out object? rawValue))
            {
                continue;
            }

            object? convertedValue =
                ConvertToType(
                    rawValue,
                    property.PropertyType);

            property.SetValue(
                instance,
                convertedValue);
        }

        return instance;
    }


    private static object ConvertCollection(
    List<object?> values,
    Type targetType)
    {
        if (targetType.IsArray)
        {
            Type elementType = targetType.GetElementType()!;

            Array array =
                Array.CreateInstance(
                    elementType,
                    values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                object? convertedValue =
                    ConvertToType(
                        values[i],
                        elementType);

                array.SetValue(
                    convertedValue,
                    i);
            }

            return array;
        }

        if (targetType.IsGenericType)
        {
            Type genericType =
                targetType.GetGenericTypeDefinition();

            if (genericType == typeof(List<>) ||
                genericType == typeof(IEnumerable<>))
            {
                Type elementType =
                    targetType.GetGenericArguments()[0];

                Type listType =
                    typeof(List<>).MakeGenericType(elementType);

                var list =
                    Activator.CreateInstance(listType)!;

                MethodInfo addMethod =
                    listType.GetMethod("Add")!;

                foreach (object? value in values)
                {
                    object? convertedValue =
                        ConvertToType(
                            value,
                            elementType);

                    addMethod.Invoke(
                        list,
                        new[] { convertedValue });
                }

                return list;
            }
        }

        throw new JsonSerializationException(
            $"Cannot convert JSON array to '{targetType.Name}'.");
    }

    private static bool IsDictionaryType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        Type genericType =
            type.GetGenericTypeDefinition();

        return genericType == typeof(Dictionary<,>) ||
               genericType == typeof(IDictionary<,>);
    }

    private static object ConvertDictionary(
    Dictionary<string, object?> dictionary,
    Type targetType)
    {
        Type[] genericArguments =
            targetType.GetGenericArguments();

        Type keyType = genericArguments[0];
        Type valueType = genericArguments[1];

        if (keyType != typeof(string))
        {
            throw new JsonSerializationException(
                "Dictionary keys must be strings.");
        }

        Type dictionaryType =
            typeof(Dictionary<,>)
                .MakeGenericType(
                    typeof(string),
                    valueType);

        var result =
            Activator.CreateInstance(dictionaryType)!;

        MethodInfo addMethod =
            dictionaryType.GetMethod("Add")!;

        foreach (var entry in dictionary)
        {
            object? convertedValue =
                ConvertToType(
                    entry.Value,
                    valueType);

            addMethod.Invoke(
                result,
                new[]
                {
                entry.Key,
                convertedValue
                });
        }

        return result;
    }
}

