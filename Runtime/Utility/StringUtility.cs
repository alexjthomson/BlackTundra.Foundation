using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BlackTundra.Foundation.Utility {

    public static class StringUtility {

        #region constant

        public static readonly string NewLine = Environment.NewLine;

        #endregion

        #region logic

        #region ToFormattedString

        public static string ToFormattedString(this DateTime instance) => new string(instance.ToFormattedCharArray());

        #endregion

        #region ToFormattedCharArray

        public static char[] ToFormattedCharArray(this DateTime instance) {

            char[] timestamp = new char[19];

            // date:

            int year = instance.Year; // entire year
            int yearFrag = year / 100; // first 2 characters of the year

            Append2CharIntToCharArray(timestamp, 0, yearFrag); // first 2 digits
            Append2CharIntToCharArray(timestamp, 2, year - (yearFrag * 100)); // last 2 digits

            timestamp[4] = '-';
            Append2CharIntToCharArray(timestamp, 5, instance.Month);

            timestamp[7] = '-';
            Append2CharIntToCharArray(timestamp, 8, instance.Day);

            // seperator:

            timestamp[10] = ' ';

            // time:

            Append2CharIntToCharArray(timestamp, 11, instance.Hour);
            timestamp[13] = ':';
            Append2CharIntToCharArray(timestamp, 14, instance.Minute);
            timestamp[16] = ':';
            Append2CharIntToCharArray(timestamp, 17, instance.Second);

            return timestamp;

        }

        #endregion

        #region Append2CharIntToCharArray

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Append2CharIntToCharArray(in char[] instance, in int index, in int value) {

            instance[index] = (char)((value / 10) + '0');
            instance[index + 1] = (char)((value % 10) + '0');

        }

        #endregion

        #region Escape

        public static string Escape(this string instance) {

            StringBuilder builder = new StringBuilder(instance);
            builder.Replace("\\", "\\\\");
            builder.Replace("\a", "\\a");
            builder.Replace("\b", "\\b");
            builder.Replace("\f", "\\f");
            builder.Replace("\n", "\\n");
            builder.Replace("\r", "\\r");
            builder.Replace("\t", "\\t");
            builder.Replace("\v", "\\v");
            builder.Replace("\'", "\\'");
            builder.Replace("\"", "\\\"");
            builder.Replace("?", "\\?");
            return builder.ToString();

        }

        #endregion

        #region Unescape

        public static string Unescape(this string instance) {

            StringBuilder builder = new StringBuilder(instance);
            builder.Replace("\\a", "\a");
            builder.Replace("\\b", "\b");
            builder.Replace("\\f", "\f");
            builder.Replace("\\n", "\n");
            builder.Replace("\\r", "\r");
            builder.Replace("\\t", "\t");
            builder.Replace("\\v", "\v");
            builder.Replace("\\'", "\'");
            builder.Replace("\\\"", "\"");
            builder.Replace("\\?", "?");
            builder.Replace("\\\\", "\\");
            return builder.ToString();

        }

        #endregion

        #region IsNullOrEmpty

        public static bool IsNullOrEmpty(this string instance) { return string.IsNullOrEmpty(instance); }

        #endregion

        #region IsNullOrWhitespace

        public static bool IsNullOrWhitespace(this string instance) { return string.IsNullOrWhiteSpace(instance); }

        #endregion

        #region HexToInt

        public static int HexToInt(this string value) {

            int sign = 1;

            if (value[0] == '-') {
                sign = -1;
                value = value.Substring(1);
            }

            return sign * int.Parse(value.StartsWith("0x") ? value.Substring(2) : value, NumberStyles.HexNumber);

        }

        #endregion

        #region ToHex

        public static string ToHex(this short value) => value.ToString("x4");
        public static string ToHex(this ushort value) => value.ToString("x4");
        public static string ToHex(this int value) => value.ToString("x8");
        public static string ToHex(this uint value) => value.ToString("x8");
        public static string ToHex(this long value) => value.ToString("x16");
        public static string ToHex(this ulong value) => value.ToString("x16");
        public static string ToHex(this float value) => value.ToString("x8");
        public static string ToHex(this double value) => value.ToString("x16");
        public static string ToHex(this bool value) => value ? "ff" : "00";
        public static string ToHex(this byte[] bytes) => BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
        public static string ToHex(this string value) => BitConverter.ToString(Encoding.UTF8.GetBytes(value)).Replace("-", string.Empty);

        #endregion

        #region Matches

        public static bool Matches(this string value, in string regex) => Regex.IsMatch(value ?? throw new ArgumentNullException("value"), regex ?? throw new ArgumentNullException("regex"));

        public static bool Matches(this string value, in char[] charset) {

            if (value == null) throw new ArgumentNullException("value");
            if (charset == null) throw new ArgumentNullException("charset");
            if (value.Length == 0) return true;
            if (charset.Length == 0) return false;

            for (int i = 0; i < value.Length; i++) {

                char c = value[i];
                bool match = false;
                for (int j = 0; j < charset.Length; j++) {

                    if (c == charset[j]) {
                        match = true;
                        break;
                    }

                }

                if (!match) return false; // current character didn't match any character in the charset

            }

            return true; // all tests passed

        }

        #endregion

        #endregion

    }

}