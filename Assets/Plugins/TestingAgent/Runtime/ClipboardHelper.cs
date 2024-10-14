using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TestingAgent
{
    public static class ClipboardHelper
    {
        public static void CopyAsRows<T>(IList<StringLine<T>> lines) => GUIUtility.systemCopyBuffer = FormatDataAsRows(lines);

        public static void Copy(string value) => GUIUtility.systemCopyBuffer = value;

        public static string CopyAsColumns<T>(IList<StringLine<T>> columns)
        {
            if(columns == null || columns.Count == 0)
                return string.Empty;

            int cols = columns.Count;
            int rows = columns[0].List.Count;

            StringBuilder builder = new();

            bool hasAnyHeader = false;
            
            for (int i = 0; i < cols; i++)
            {
                if (hasAnyHeader && string.IsNullOrEmpty(columns[i].Header))
                {
                    string append = i switch
                    {
                        _ when i == cols - 1 => $"\r",
                        _ => $"\t"
                    };

                    builder.Append(append);
                    continue;
                }

                if (!string.IsNullOrEmpty(columns[i].Header))
                {
                    string append = i switch
                    {
                        _ when i == cols - 1 => $"{columns[i].Header}\r",
                        _ => $"{columns[i].Header}\t"
                    };
                    
                    builder.Append($"{append}");
                    hasAnyHeader = true;
                }
            }
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Func<T, string> action = columns[j].Action;
                    
                    string append = j switch
                    {
                        _ when j == cols - 1 => $"{action(columns[j].List[i])}\r",
                        _ => $"{action(columns[j].List[i])}\t"
                    };

                    builder.Append($"{append}");
                }
            }
            
            GUIUtility.systemCopyBuffer = builder.ToString();
            return GUIUtility.systemCopyBuffer;
        }

        public static string FormatDataAsRows<T>(IList<StringLine<T>> lines)
        {
            if(lines == null || lines.Count == 0)
                return string.Empty;

            StringBuilder builder = new();
            
            bool hasAnyHeader = lines.Any(x => !string.IsNullOrEmpty(x.Header));
            
            builder.Append(ConstructRow(lines[0].List, lines[0].Action, lines[0].Header, false, hasAnyHeader));

            for (int i = 1; i < lines.Count; i++)
                builder.Append(ConstructRow(lines[i].List, lines[i].Action, lines[i].Header, true, hasAnyHeader));

            return builder.ToString();
        }
        
        private static string ConstructRow<T>(IList<T> list, Func<T, string> info, string header, bool newLine, bool hasAnyHeader)
        {
            StringBuilder builder = new();
                
            if(newLine)
                builder.Append("\r");

            
            if (!string.IsNullOrEmpty(header))
            {
                builder.Append($"{header}\t");
            }
            else
            {
                if(hasAnyHeader)
                    builder.Append("\t");
            }
            
            for (int i = 0; i < list.Count; i++)
            {
                string append = i switch
                {
                    _ when i == list.Count - 1 => $"{info(list[i])}",
                    _ => $"{info(list[i])}\t"
                };
                
                builder.Append(append);
            }

            return builder.ToString();
        }
        
        public class StringLine<T>
        {
            public readonly List<T> List;
            public readonly Func<T, string> Action;
            public readonly string Header;

            public StringLine(List<T> list, Func<T, string> action, string header = null)
            {
                List = list;
                Action = action;
                Header = header;
            }
        }
    }
}