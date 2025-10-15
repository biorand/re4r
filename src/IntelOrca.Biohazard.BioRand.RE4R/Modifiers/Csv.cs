using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal static class Csv
    {
        public static T[] Deserialize<T>(byte[] utf8Data)
        {
            var tokens = ReadTokens(utf8Data);

            var typ = typeof(T);
            var propertyMap = new List<PropertyInfo?>();
            var keyProperty = typ.GetProperties().FirstOrDefault(static x => x.GetCustomAttribute<KeyAttribute>() != null);
            var x = 0;
            var y = 0;

            var results = new List<T>();
            T? element = default;
            foreach (var t in tokens)
            {
                if (t.Kind == TokenKind.EOF)
                {
                    if (element != null)
                    {
                        results.Add(element);
                    }
                    break;
                }
                else if (t.Kind == TokenKind.Comma)
                {
                    x++;
                }
                else if (t.Kind == TokenKind.NewLine)
                {
                    if (element != null)
                    {
                        results.Add(element);
                    }
                    y++;
                    x = 0;
                    element = Activator.CreateInstance<T>();
                    keyProperty?.SetValue(element, y + 1);
                }
                else if (t.Kind == TokenKind.Text)
                {
                    if (y == 0)
                    {
                        while (propertyMap.Count <= x)
                        {
                            propertyMap.Add(null);
                        }
                        propertyMap[x] = typ.GetProperty(t.Text, BindingFlags.Public | BindingFlags.Instance);
                    }
                    else
                    {
                        var prop = propertyMap[x];
                        if (prop != null)
                        {
                            if (!string.IsNullOrWhiteSpace(t.Text))
                            {
                                prop.SetValue(element, ParseValue(t.Text, prop.PropertyType));
                            }
                        }
                    }
                }
            }
            return results.ToArray();
        }

        internal static readonly string[] g_separator = ["\r\n", "\n"];

        private static object ParseValue(string input, Type targetType)
        {
            if (targetType == typeof(ImmutableArray<string>))
            {
                return input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            }
            else if (targetType == typeof(Guid))
            {
                return Guid.Parse(input);
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, input, ignoreCase: true);
            }
            else
            {
                return Convert.ChangeType(input, targetType);
            }
        }

        public static string[,] Read(string data)
        {
            var rows = new List<string[]>();
            var columns = new List<string>();

            var sb = new StringBuilder();
            var inQuote = false;
            for (var i = 0; i <= data.Length; i++)
            {
                var c = i == data.Length ? '\0' : data[i];
                if (c == '"')
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                    }
                    else
                    {
                        if (i < data.Length - 1 && data[i + 1] == '"')
                        {
                            sb.Append('"');
                        }
                        else
                        {
                            inQuote = false;
                        }
                    }
                    continue;
                }
                else if (c == ',')
                {
                    if (inQuote)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        columns.Add(sb.ToString());
                        sb.Clear();
                    }
                    continue;
                }
                else if (!inQuote && c == '\r')
                {
                    // Check if next char is \n
                    if (data.Length > i + 1)
                    {
                        if (data[i + 1] == '\n')
                        {
                            // leave handling to next char
                            continue;
                        }
                    }
                }

                if ((!inQuote && (c == '\r' || c == '\n')) || c == '\0')
                {
                    columns.Add(sb.ToString());
                    sb.Clear();
                    rows.Add(columns.ToArray());
                    columns.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            var numRows = rows.Count;
            var numColumns = rows.Max(x => x.Length);
            var result = new string[numColumns, numRows];
            for (var y = 0; y < numRows; y++)
            {
                var row = rows[y];
                for (var x = 0; x < numColumns; x++)
                {
                    result[x, y] = row.Length > x ? row[x] : "";
                }
            }
            return result;
        }

        private static void SplitLine(List<string> list, StringBuilder sb, string line)
        {
            list.Clear();
            var inQuote = false;
            for (var i = 0; i <= line.Length; i++)
            {
                var c = i == line.Length ? '\0' : line[i];
                if (c == '"')
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                    }
                    else
                    {
                        if (i < line.Length - 1 && line[i + 1] == '"')
                        {
                            sb.Append('"');
                        }
                        else
                        {
                            inQuote = false;
                        }
                    }
                }
                else if (c == ',')
                {
                    if (inQuote)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (c == '\0')
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        private static ImmutableArray<Token> ReadTokens(byte[] buffer)
        {
            var tokens = ImmutableArray.CreateBuilder<Token>();
            var inQuotes = false;
            var textStart = -1;
            for (var i = 0; i < buffer.Length; i++)
            {
                var b = buffer[i];
                if (!inQuotes)
                {
                    if (b == ',')
                    {
                        Finish(i);
                        tokens.Add(new Token(TokenKind.Comma, ","));
                    }
                    else if (b == '\n')
                    {
                        Finish(i);
                        tokens.Add(new Token(TokenKind.NewLine, "\n"));
                    }
                    else if (b == '\r')
                    {
                        Finish(i);
                        if (i + 1 < buffer.Length)
                        {
                            if (buffer[i + 1] == '\n')
                            {
                                tokens.Add(new Token(TokenKind.NewLine, "\r\n"));
                                i++;
                            }
                        }
                    }
                    else if (b == '"')
                    {
                        inQuotes = true;
                    }
                    else if (textStart == -1)
                    {
                        textStart = i;
                    }
                }
                else
                {
                    if (b == '"')
                    {
                        if (i + 1 < buffer.Length)
                        {
                            if (buffer[i + 1] == '"')
                            {
                                i++;
                                continue;
                            }
                        }
                        Finish(i);
                        inQuotes = false;
                    }
                    else if (textStart == -1)
                    {
                        textStart = i;
                    }
                }
            }
            Finish(buffer.Length);
            tokens.Add(new Token(TokenKind.EOF, "\0"));
            return tokens.ToImmutable();

            void Finish(int position)
            {
                if (textStart == -1)
                {
                    tokens.Add(new Token(TokenKind.Text, ""));
                }
                else
                {
                    var length = position - textStart;
                    var text = Encoding.UTF8.GetString(buffer, textStart, length)
                        .Replace("\"\"", "\"");
                    tokens.Add(new Token(TokenKind.Text, text));
                    textStart = -1;
                }
            }
        }

        [DebuggerDisplay("{Kind} | {Text}")]
        private readonly struct Token(TokenKind kind, string text)
        {
            public TokenKind Kind { get; } = kind;
            public string Text { get; } = text;
        }

        private enum TokenKind
        {
            Unknown,
            Text,
            Comma,
            NewLine,
            EOF
        }
    }
}
