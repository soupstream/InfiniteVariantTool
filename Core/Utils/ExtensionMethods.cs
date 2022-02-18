using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace InfiniteVariantTool.Core.Utils
{
    public static class MyExtensions
    {
        public static string GetText(this XElement xel)
        {
            XNode? node = xel.Nodes().FirstOrDefault(child => child is XText);
            if (node is XText textNode)
            {
                return textNode.Value;
            }
            else
            {
                return "";
            }
        }

        public static void SetText(this XElement xel, string text)
        {
            XNode? node = xel.Nodes().FirstOrDefault(child => child is XText);
            if (node is XText textNode)
            {
                textNode.Value = text;
            }
            else
            {
                xel.Add(new XText(text));
            }
        }

        public static TEnum? ToEnum<TEnum>(this string value) where TEnum : struct
        {
            return Enum.TryParse(value, out TEnum result) ? result : null;
        }

        public static bool TryFirst<TSource>(this IEnumerable<TSource> source, [MaybeNullWhen(false)] out TSource value, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    value = item;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool TryFirst<TSource>(this IEnumerable<TSource> source, [MaybeNullWhen(false)] out TSource value)
        {
            if (source.Any())
            {
                value = source.First();
                return true;
            }
            value = default;
            return false;
        }

        public static XProcessingInstruction? GetProcessingInstruction(this XDocument doc)
        {
            return doc.Nodes()
                .OfType<XProcessingInstruction>()
                .FirstOrDefault(node => node.Target == Constants.AppName);
        }

        public static Dictionary<string, string> GetAttributes(this XProcessingInstruction xpi)
        {
            // not 100% robust but good enough
            var matches = Regex.Matches(xpi.Data, "([a-zA-Z0-9_]*) *= *\"([^\"]*)");
            Dictionary<string, string> attributes = new();
            foreach (Match match in matches)
            {
                attributes[match.Groups[1].Value] = match.Groups[2].Value;
            }
            return attributes;
        }

        public static void SetAttributes(this XProcessingInstruction xpi, Dictionary<string, string> attributes)
        {
            xpi.Data = string.Join(' ', attributes.Select(entry => $"{entry.Key}=\"{entry.Value}\""));
        }

        public static string? GetAttribute(this XProcessingInstruction xpi, string key)
        {
            var attributes = xpi.GetAttributes();
            return attributes.GetValueOrDefault(key);
        }

        public static void SetAttribute(this XProcessingInstruction xpi, string key, string value)
        {
            var attributes = xpi.GetAttributes();
            attributes[key] = value;
            xpi.SetAttributes(attributes);
        }

        public static string? GetVersion(this XDocument doc)
        {
            return doc.GetProcessingInstruction()?.GetAttribute("version");
        }

        public static void SetProcessingInstructionAttribute(this XDocument doc, string key, string value)
        {
            XProcessingInstruction? xpi = doc.GetProcessingInstruction(); 
            if (xpi == null)
            {
                xpi = new XProcessingInstruction(Constants.AppName, "");
                doc.AddFirst(xpi);
            }
            xpi.SetAttribute(key, value);
        }

        public static void SetVersion(this XDocument doc, string version)
        {
            doc.SetProcessingInstructionAttribute("version", version);
        }

        public static void SetVersion(this XDocument doc)
        {
            string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            if (version != null)
            {
                doc.SetVersion(version);
            }
        }

        public static void SaveVersioned(this XDocument doc, string filename)
        {
            doc.SetVersion();
            doc.Save(filename);
        }

        public static void SaveVersioned(this XElement xel, string filename)
        {
            XDocument doc = new(xel);
            doc.SetVersion();
            doc.Save(filename);
        }
    }
}
