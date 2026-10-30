using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WaaS.Runtime.Debugging
{
    /// <summary>
    ///     A minimal JSON reader and writer for the messages of the Debug Adapter Protocol. Objects are
    ///     <see cref="Dictionary{TKey,TValue}" /> of string and object, arrays are <see cref="List{T}" /> of object, and numbers
    ///     are <see cref="double" />.
    /// </summary>
    internal static class Json
    {
        public static object Parse(string text)
        {
            var position = 0;
            var value = ParseValue(text, ref position);
            SkipWhitespace(text, ref position);
            if (position != text.Length) throw new FormatException("unexpected characters after the JSON value");
            return value;
        }

        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value);
            return builder.ToString();
        }

        private static object ParseValue(string text, ref int position)
        {
            SkipWhitespace(text, ref position);
            if (position >= text.Length) throw new FormatException("unexpected end of JSON");
            switch (text[position])
            {
                case '{':
                {
                    position++;
                    var result = new Dictionary<string, object>();
                    SkipWhitespace(text, ref position);
                    if (text[position] == '}')
                    {
                        position++;
                        return result;
                    }

                    while (true)
                    {
                        SkipWhitespace(text, ref position);
                        var key = ParseString(text, ref position);
                        SkipWhitespace(text, ref position);
                        Expect(text, ref position, ':');
                        result[key] = ParseValue(text, ref position);
                        SkipWhitespace(text, ref position);
                        if (text[position] == ',')
                        {
                            position++;
                            continue;
                        }

                        Expect(text, ref position, '}');
                        return result;
                    }
                }
                case '[':
                {
                    position++;
                    var result = new List<object>();
                    SkipWhitespace(text, ref position);
                    if (text[position] == ']')
                    {
                        position++;
                        return result;
                    }

                    while (true)
                    {
                        result.Add(ParseValue(text, ref position));
                        SkipWhitespace(text, ref position);
                        if (text[position] == ',')
                        {
                            position++;
                            continue;
                        }

                        Expect(text, ref position, ']');
                        return result;
                    }
                }
                case '"':
                    return ParseString(text, ref position);
                case 't':
                    ExpectWord(text, ref position, "true");
                    return true;
                case 'f':
                    ExpectWord(text, ref position, "false");
                    return false;
                case 'n':
                    ExpectWord(text, ref position, "null");
                    return null;
                default:
                {
                    var start = position;
                    while (position < text.Length && "+-0123456789.eE".IndexOf(text[position]) >= 0) position++;
                    if (start == position) throw new FormatException($"unexpected character '{text[position]}'");
                    return double.Parse(text.Substring(start, position - start), CultureInfo.InvariantCulture);
                }
            }
        }

        private static string ParseString(string text, ref int position)
        {
            Expect(text, ref position, '"');
            var builder = new StringBuilder();
            while (true)
            {
                var c = text[position++];
                if (c == '"') return builder.ToString();
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                var escaped = text[position++];
                switch (escaped)
                {
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
                        builder.Append((char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber));
                        position += 4;
                        break;
                    default:
                        builder.Append(escaped);
                        break;
                }
            }
        }

        private static void SkipWhitespace(string text, ref int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
        }

        private static void Expect(string text, ref int position, char c)
        {
            if (position >= text.Length || text[position] != c) throw new FormatException($"'{c}' is expected");
            position++;
        }

        private static void ExpectWord(string text, ref int position, string word)
        {
            if (string.CompareOrdinal(text, position, word, 0, word.Length) != 0)
                throw new FormatException($"'{word}' is expected");
            position += word.Length;
        }

        private static void Write(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case string s:
                    WriteString(builder, s);
                    break;
                case bool b:
                    builder.Append(b ? "true" : "false");
                    break;
                case IDictionary<string, object> dictionary:
                {
                    builder.Append('{');
                    var first = true;
                    foreach (var pair in dictionary)
                    {
                        if (!first) builder.Append(',');
                        first = false;
                        WriteString(builder, pair.Key);
                        builder.Append(':');
                        Write(builder, pair.Value);
                    }

                    builder.Append('}');
                    break;
                }
                case IEnumerable enumerable:
                {
                    builder.Append('[');
                    var first = true;
                    foreach (var item in enumerable)
                    {
                        if (!first) builder.Append(',');
                        first = false;
                        Write(builder, item);
                    }

                    builder.Append(']');
                    break;
                }
                case IFormattable formattable:
                    builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    break;
                default:
                    WriteString(builder, value.ToString());
                    break;
            }
        }

        private static void WriteString(StringBuilder builder, string s)
        {
            builder.Append('"');
            foreach (var c in s)
                switch (c)
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
                    default:
                        if (c < 0x20) builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }

            builder.Append('"');
        }
    }
}
