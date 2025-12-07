using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal static class Csv
    {
        public static T[] Deserialize<T>(byte[] utf8Data)
        {
            return Deserialize<T>(Encoding.UTF8.GetString(utf8Data));
        }

        internal static readonly string[] g_separator = ["\r\n", "\n"];

        public static T[] Deserialize<T>(string data)
        {
            var lines = data.Split(g_separator, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
                return [];

            var typ = typeof(T);
            var result = new List<T>();
            var sb = new StringBuilder();
            var columns = new List<string>();
            SplitLine(columns, sb, lines[0]);
            var mapping = GetPropertyMapping(typ, columns, out var keyProperty);
            for (var i = 1; i < lines.Length; i++)
            {
                SplitLine(columns, sb, lines[i]);

                var element = Activator.CreateInstance<T>();
                keyProperty?.SetValue(element, i + 1);
                for (var j = 0; j < columns.Count; j++)
                {
                    var prop = mapping[j];
                    if (prop != null)
                    {
                        var text = columns[j];
                        if (!string.IsNullOrEmpty(text))
                        {
                            prop.SetValue(element, ParseValue(columns[j], prop.PropertyType));
                        }
                    }
                }
                result.Add(element);
            }
            return result.ToArray();
        }

        private static PropertyInfo?[] GetPropertyMapping(Type typ, IEnumerable<string> columns, out PropertyInfo? keyProperty)
        {
            keyProperty = typ.GetProperties().FirstOrDefault(static x => x.GetCustomAttribute<KeyAttribute>() != null);
            return columns.Select(typ.GetProperty).ToArray();
        }

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
                return Enum.Parse(targetType, input);
            }
            else
            {
                return Convert.ChangeType(input, targetType);
            }
        }

        public static string[,] Read(string data)
        {
            var lines = data.Split(g_separator, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 0)
                return new string[0, 0];

            var sb = new StringBuilder();
            var rows = new List<string[]>();
            var columns = new List<string>();
            for (var i = 1; i < lines.Length; i++)
            {
                SplitLine(columns, sb, lines[i]);
                rows.Add(columns.ToArray());
            }

            var numRows = rows.Count;
            var numColumns = columns.Count;
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
    }
}
