# Custom JSON Serializer

A custom JSON serializer and deserializer implemented from scratch in C# without using `System.Text.Json`, Newtonsoft.Json, or other JSON libraries for the core functionality.

## Features

* Serialize primitive values
* Serialize objects using reflection
* Serialize nested objects
* Serialize arrays and generic collections
* Serialize `List<T>` and `IEnumerable<T>`
* Serialize `Dictionary<string, object>`
* Deserialize JSON into strongly typed C# objects
* Deserialize nested objects and collections
* Support for:

  * `DateTime`
  * `Guid`
  * Enums
  * Nullable value types
* JSON string escaping and Unicode escape parsing
* Malformed JSON detection
* Type conversion and validation
* Circular reference detection
* Reflection metadata caching for better performance

## Project Structure

```text
CustomJsonSerializer
│
├── JsonSerializer.cs
├── Serialization
│   └── ReflectionCache.cs
├── Parsing
│   └── JsonParser.cs
├── Conversion
│   └── JsonConverter.cs
└── Exceptions
    └── JsonSerializationException.cs
```

## Basic Usage

### Serialization

```csharp
User user = new User
{
    Id = 1,
    Name = "John",
    IsActive = true
};

string json = JsonSerializer.Serialize(user);

Console.WriteLine(json);
```

Example output:

```json
{"Id":1,"Name":"John","IsActive":true}
```

### Deserialization

```csharp
string json = """
{
    "Id": 1,
    "Name": "John",
    "IsActive": true
}
""";

User? user = JsonSerializer.Deserialize<User>(json);

Console.WriteLine(user?.Name);
```

## Architecture

The implementation is divided into several responsibilities:

### JsonSerializer

Provides the public API and controls serialization and deserialization.

### JsonParser

Reads raw JSON text and converts it into basic C# representations such as:

* `Dictionary<string, object?>`
* `List<object?>`
* `string`
* `long`
* `double`
* `bool`
* `null`

### JsonConverter

Converts the parsed JSON representation into the requested C# type using reflection and type conversion.

### ReflectionCache

Caches reflection metadata for each type to avoid repeatedly calling reflection APIs during serialization.

### JsonSerializationException

Custom exception used for JSON parsing, serialization, and conversion errors.

## Circular References

Circular references are detected using a reference-based `HashSet`.

For example:

```text
Person
  └── Friend
       └── Person
            └── Friend ...
```

Instead of causing infinite recursion, the serializer throws a `JsonSerializationException`.

## Error Handling

The parser detects malformed JSON including:

* Missing braces
* Missing brackets
* Missing colons
* Missing commas
* Unterminated strings
* Invalid escape sequences
* Invalid Unicode escapes
* Invalid boolean/null values
* Invalid numbers

The converter also reports incompatible JSON and C# types.

## Limitations

This project is an educational implementation and does not aim to provide all features of production JSON libraries.

It focuses on the core JSON serialization, parsing, deserialization, reflection, collections, error handling, and performance requirements of the assignment.

## Technologies

* C#
* .NET
* Reflection
* Generics
* Collections
* `StringBuilder`
* `ConcurrentDictionary`
