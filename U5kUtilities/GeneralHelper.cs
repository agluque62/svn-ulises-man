using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class GeneralHelper
    {
        public static T SafeStringToEnum<T>(string input)
        {
            if (Enum.IsDefined(typeof(T), input))
            {
                var val = Enum.Parse(typeof(T), input);
                return (T)val;
            }
            return (T)(object)0;
        }
        public static T SafeIntToEnum<T>(int input)
        {
            if (Enum.IsDefined(typeof(T), input))
            {
                var val = Enum.Parse(typeof(T), input.ToString());
                return (T)val;
            }
            return (T)(object)0;
        }
        public static string ToShow(string str, int max)
        {
            if (str.Length <= max)
            {
                return str;
            }
            var segment = max / 2;
            return $"{str.Substring(0, segment)} ... {str.Substring(str.Length - segment, segment)}";
        }
        public static string NormalizeWhiteSpace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int current = 0;
            char[] output = new char[input.Length];
            bool skipped = false;

            foreach (char c in input.ToCharArray())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!skipped)
                    {
                        if (current > 0)
                            output[current++] = ' ';

                        skipped = true;
                    }
                }
                else
                {
                    skipped = false;
                    output[current++] = c;
                }
            }

            return new string(output, 0, current);
        }
        public static string[] SplitToChunks(string source, int maxLength)
        {
            return source
                .Where((x, i) => i % maxLength == 0)
                .Select(
                    (x, i) => new string(source
                        .Skip(i * maxLength)
                        .Take(maxLength)
                        .ToArray()))
                .ToArray();
        }
    }
}
