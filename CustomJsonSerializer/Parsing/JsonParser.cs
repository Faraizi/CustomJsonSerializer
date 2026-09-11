using CustomJsonSerializer.Exceptions;
using System.Globalization;
using System.Text;

namespace CustomJsonSerializer.Parsing;

public class JsonParser
{
    private readonly string _json;
    private int _position;

    public JsonParser(string json)
    {
        _json = json;
        _position = 0;
    }

    public object? Parse()
    {
        SkipWhitespace();

        object? value = ParseValue();

        SkipWhitespace();

        if (!IsEnd())
        {
            throw Error(
                $"Unexpected character '{Current}'.");
        }

        return value;
    }

    private object? ParseValue()
    {
        SkipWhitespace();

        if (IsEnd())
        {
            throw Error("Expected a JSON value.");
        }

        return Current switch
        {
            '{' => ParseObject(),
            '[' => ParseArray(),
            '"' => ParseString(),

            't' or 'f' => ParseBoolean(),

            'n' => ParseNull(),

            '-' => ParseNumber(),

            _ when char.IsDigit(Current)
                => ParseNumber(),

            _ => throw Error(
                $"Unexpected character '{Current}'.")
        };
    }

    private object ParseObject()
    {
        Dictionary<string, object?> result = new();

        if (Current != '{')
        {
            throw Error("Expected '{'.");
        }

        _position++;

        SkipWhitespace();

        if (!IsEnd() && Current == '}')
        {
            _position++;
            return result;
        }

        while (true)
        {
            SkipWhitespace();

            if (IsEnd() || Current != '"')
            {
                throw Error(
                    "Expected a property name.");
            }

            string propertyName =
                ParseString();

            SkipWhitespace();

            if (IsEnd() || Current != ':')
            {
                throw Error(
                    "Expected ':' after property name.");
            }

            _position++;

            SkipWhitespace();

            object? propertyValue =
                ParseValue();

            if (!result.TryAdd(
                propertyName,
                propertyValue))
            {
                throw Error(
                    $"Duplicate property '{propertyName}'.");
            }

            SkipWhitespace();

            if (IsEnd())
            {
                throw Error(
                    "Unexpected end of JSON object.");
            }

            if (Current == '}')
            {
                _position++;
                return result;
            }

            if (Current != ',')
            {
                throw Error(
                    "Expected ',' or '}' in object.");
            }

            _position++;
        }
    }

    private object ParseArray()
    {
        List<object?> result = new();

        if (Current != '[')
        {
            throw Error("Expected '['.");
        }

        _position++;

        SkipWhitespace();

        if (!IsEnd() && Current == ']')
        {
            _position++;
            return result;
        }

        while (true)
        {
            SkipWhitespace();

            object? value =
                ParseValue();

            result.Add(value);

            SkipWhitespace();

            if (IsEnd())
            {
                throw Error(
                    "Unexpected end of JSON array.");
            }

            if (Current == ']')
            {
                _position++;
                return result;
            }

            if (Current != ',')
            {
                throw Error(
                    "Expected ',' or ']' in array.");
            }

            _position++;
        }
    }

    private string ParseString()
    {
        if (Current != '"')
        {
            throw Error("Expected '\"'.");
        }

        _position++;

        StringBuilder builder = new();

        while (!IsEnd())
        {
            char character = Current;

            if (character == '"')
            {
                _position++;
                return builder.ToString();
            }

            if (character == '\\')
            {
                _position++;

                if (IsEnd())
                {
                    throw Error(
                        "Unexpected end of JSON inside string.");
                }

                char escaped = Current;

                switch (escaped)
                {
                    case '"':
                        builder.Append('"');
                        break;

                    case '\\':
                        builder.Append('\\');
                        break;

                    case '/':
                        builder.Append('/');
                        break;

                    case 'b':
                        builder.Append('\b');
                        break;

                    case 'f':
                        builder.Append('\f');
                        break;

                    case 'n':
                        builder.Append('\n');
                        break;

                    case 'r':
                        builder.Append('\r');
                        break;

                    case 't':
                        builder.Append('\t');
                        break;

                    case 'u':
                        builder.Append(ParseUnicodeEscape());
                        break;

                    default:
                        throw Error(
                            $"Invalid escape sequence '\\{escaped}'.");
                }

                _position++;
                continue;
            }

            if (character < 0x20)
            {
                throw Error(
                    "Unescaped control character inside JSON string.");
            }

            builder.Append(character);
            _position++;
        }

        throw Error(
            "Unterminated JSON string.");
    }

    private object ParseNumber()
    {
        int start = _position;

        if (Current == '-')
        {
            _position++;
        }

        if (IsEnd() || !char.IsDigit(Current))
        {
            throw Error("Invalid number.");
        }

        if (Current == '0')
        {
            _position++;
        }
        else
        {
            while (!IsEnd() &&
                   char.IsDigit(Current))
            {
                _position++;
            }
        }

        bool isDecimal = false;

        if (!IsEnd() && Current == '.')
        {
            isDecimal = true;
            _position++;

            if (IsEnd() || !char.IsDigit(Current))
            {
                throw Error(
                    "A decimal point must be followed by digits.");
            }

            while (!IsEnd() &&
                   char.IsDigit(Current))
            {
                _position++;
            }
        }

        if (!IsEnd() &&
            (Current == 'e' || Current == 'E'))
        {
            isDecimal = true;

            _position++;

            if (!IsEnd() &&
                (Current == '+' || Current == '-'))
            {
                _position++;
            }

            if (IsEnd() || !char.IsDigit(Current))
            {
                throw Error(
                    "Invalid exponent in number.");
            }

            while (!IsEnd() &&
                   char.IsDigit(Current))
            {
                _position++;
            }
        }

        string numberText =
            _json[start.._position];

        if (isDecimal)
        {
            if (double.TryParse(
                numberText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double doubleValue))
            {
                return doubleValue;
            }
        }
        else
        {
            if (long.TryParse(
                numberText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long longValue))
            {
                return longValue;
            }
        }

        throw Error(
            $"Invalid number '{numberText}'.");
    }

    private bool ParseBoolean()
    {
        if (_json.AsSpan(_position)
            .StartsWith("true"))
        {
            _position += 4;
            return true;
        }

        if (_json.AsSpan(_position)
            .StartsWith("false"))
        {
            _position += 5;
            return false;
        }

        throw Error(
            "Invalid boolean value.");
    }

    private object? ParseNull()
    {
        Expect("null");

        return null;
    }

    private void SkipWhitespace()
    {
        while (!IsEnd() && char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }

    private bool IsEnd()
    {
        return _position >= _json.Length;
    }

    private char Current
    {
        get
        {
            if (IsEnd())
            {
                throw Error("Unexpected end of JSON.");
            }

            return _json[_position];
        }
    }

    private JsonSerializationException Error(string message)
    {
        return new JsonSerializationException(
            $"{message} Position: {_position}.");
    }

    private void Expect(string expected)
    {
        if (_position + expected.Length > _json.Length)
        {
            throw Error(
                $"Expected '{expected}'.");
        }

        string actual =
            _json.Substring(
                _position,
                expected.Length);

        if (actual != expected)
        {
            throw Error(
                $"Expected '{expected}'.");
        }

        _position += expected.Length;
    }

    private char ParseUnicodeEscape()
    {
        _position++;

        if (_position + 4 > _json.Length)
        {
            throw Error(
                "Incomplete Unicode escape sequence.");
        }

        string hex = _json.Substring(
            _position,
            4);

        foreach (char character in hex)
        {
            if (!Uri.IsHexDigit(character))
            {
                throw Error(
                    $"Invalid Unicode escape sequence '\\u{hex}'.");
            }
        }

        int value = Convert.ToInt32(
            hex,
            16);

        _position += 4;

        return (char)value;
    }
}