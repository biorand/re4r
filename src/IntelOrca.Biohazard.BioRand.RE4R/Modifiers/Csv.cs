﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            var mapping = columns.Select(typ.GetProperty).ToArray();
            for (var i = 1; i < lines.Length; i++)
            {
                SplitLine(columns, sb, lines[i]);

                var element = Activator.CreateInstance<T>();
                for (var j = 0; j < columns.Count; j++)
                {
                    var prop = mapping[j];
                    if (prop != null)
                    {
                        var value = Convert.ChangeType(columns[j], prop.PropertyType);
                        prop.SetValue(element, value);
                    }
                }
                result.Add(element);
            }
            return result.ToArray();

            static void SplitLine(List<string> list, StringBuilder sb, string line)
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
                    else if (c == '\0' || c == ',')
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
}
