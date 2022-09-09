using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;
using System.Reflection;
using System.Diagnostics;

namespace InfiniteVariantTool.Core.Utils
{
    public class Util
    {
        public static byte[] ListToBlob(XElement list)
        {
            List<byte> blob = new();
            foreach (var item in list.Elements())
            {
                blob.Add(byte.Parse(item.GetText()));
            }
            return blob.ToArray();
        }

        public static bool IsNodeGUID(XElement node)
        {
            if (node.Elements().Count() == 4)
            {
                BondType[] schema = { BondType.uint32, BondType.uint16, BondType.uint16, BondType.uint64 };
                for (int i = 0; i < 4; i++)
                {
                    if (node.Elements().ElementAt(i).Name != schema[i].ToString())
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool ArrayStartsWith<T>(T[] target, T[] pattern) where T : IEquatable<T>
        {
            if (target.Length < pattern.Length)
            {
                return false;
            }

            for (int i = 0; i < pattern.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(target[i], pattern[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ArrayContains<T>(T[] target, T[] pattern) where T : IEquatable<T>
        {
            for (int i = 0; i < target.Length - pattern.Length; i++)
            {
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(target[i + j], pattern[j]))
                    {
                        break;
                    }
                    else if (j == pattern.Length - 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string NullTerminate(string str)
        {
            int idx = str.IndexOf('\0');
            if (idx == -1)
            {
                return str;
            }
            else
            {
                return str[..idx];
            }
        }

        public static bool NullableSequenceEqual<T>(IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (first != null && second != null)
            {
                return first.SequenceEqual(second);
            }
            else
            {
                return first == second;
            }
        }
    }

    public class Culture : IDisposable
    {
        private readonly CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;

        public Culture(string cultureName)
            : this(CultureInfo.GetCultureInfo(cultureName))
        {
        }

        public Culture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}
