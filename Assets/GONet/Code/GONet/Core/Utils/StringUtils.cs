/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GONet.Utils
{
    public static class StringUtils
    {
        private static string[] cSharpReservedKeywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                                                           "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
                                                           "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float",
                                                           "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal",
                                                           "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
                                                           "override", "params", "private", "protected", "public", "readonly", "ref", "return",
                                                           "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
                                                           "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
                                                           "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

        /// <summary>
        /// TAKEN from .NET 4
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="separator"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        [ComVisible(false)]
        public static String Join<T>(String separator, IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            //Contract.Ensures(Contract.Result<String>() != null);
            //Contract.EndContractBlock();

            if (separator == null)
                separator = String.Empty;

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return String.Empty;

                StringBuilder result = new StringBuilder();
                if (en.Current != null)
                {
                    // handle the case that the enumeration has null entries
                    // and the case where their ToString() override is broken
                    string value = en.Current.ToString();
                    if (value != null)
                        result.Append(value);
                }

                while (en.MoveNext())
                {
                    result.Append(separator);
                    if (en.Current != null)
                    {
                        // handle the case that the enumeration has null entries
                        // and the case where their ToString() override is broken
                        string value = en.Current.ToString();
                        if (value != null)
                            result.Append(value);
                    }
                }
                return result.ToString();
            }
        }

        public static readonly string[] EOLs = new[] { "\r\n", "\n" };

        public static string[] SplitByEOL(string script)
        {
            if (string.IsNullOrEmpty(script))
            {
                return null;
            }

            return script.Split(EOLs, StringSplitOptions.None);
        }

        public static bool IsValidAccountName(string name)
        {
            string pattern = "^[a-zA-Z][a-zA-Z0-9_]{2,19}$"; // min length 2+1=3, max 19+1=20, starts with letter, alphanumeric and underscore
            return Regex.IsMatch(name, pattern);
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        public static string AddSpacesBeforeUppercase(string @string, int startIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(@string) || startIndex >= @string.Length)
            {
                return @string;
            }

            const char SPACE = ' ';
            StringBuilder result = new StringBuilder(@string.Length + 20);

            for (int i = 0; i < @string.Length; ++i)
            {
                if (i >= startIndex && char.IsUpper(@string[i]))
                {
                    result.Append(SPACE);
                }

                result.Append(@string[i]);
            }


            return result.ToString();
        }

        /// <summary>
        /// Parses a hex string into its equivalent byte array.
        /// </summary>
        /// <param name="inHex">The hex string to parse.</param>
        /// <returns>The byte equivalent of the hex string.</returns>
        public static byte[] ParseHexString(string inHex)
        {
            if (inHex == null)
            {
                throw new ArgumentNullException(nameof(inHex));
            }

            byte[] bytes;
            if ((inHex.Length & 1) != 0)
            {
                inHex = "0" + inHex; // make length of s even
            }
            bytes = new byte[inHex.Length >> 1]; // >> 1 is same as / 2
            for (int i = 0; i < bytes.Length; i++)
            {
                string hex = inHex.Substring(2 * i, 2);
                try
                {
                    byte b = Convert.ToByte(hex, 16);
                    bytes[i] = b;
                }
                catch (FormatException e)
                {
                    throw new FormatException(
                        string.Format("Invalid hex string {0}. Problem with substring {1} starting at position {2}",
                        inHex,
                        hex,
                        2 * i),
                        e);
                }
            }

            return bytes;
        }

        /// <summary>
        /// Tries to parse a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="bytes">A byte array.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexString(string s, out byte[] bytes)
        {
            try
            {
                bytes = ParseHexString(s);
            }
            catch
            {
                bytes = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// The <paramref name="appendTo"/> is appended to with a time format as follows: "MM:SS".
        /// </summary>
        /// <param name="minutes">required/expected to be between [0-59]</param>
        /// <param name="seconds">required/expected to be between [0-59]</param>
        /// <param name="appendTo">required/expected to be non-null....and for efficiency, it makes sense to have it initialized with a capacity of 5</param>
        /// <param name="shouldClearBeforeAppend"></param>
        public static void AppendFormatedTime(int minutes, int seconds, /* IN/OUT */ StringBuilder appendTo, bool shouldClearBeforeAppend = true)
        {
            const int NUMERIC_VALUES_TO_ASCII = 48;
            const string COLON = ":";

            if (shouldClearBeforeAppend)
            {
                appendTo.Remove(0, appendTo.Length);
            }

            int min1 = minutes / 10;
            int min2 = minutes % 10;
            char minutesASCII1 = (char)(min1 + NUMERIC_VALUES_TO_ASCII);
            char minutesASCII2 = (char)(min2 + NUMERIC_VALUES_TO_ASCII);

            int seconds1 = seconds / 10;
            int seconds2 = seconds % 10;
            char secondsASCII1 = (char)(seconds1 + NUMERIC_VALUES_TO_ASCII);
            char secondsASCII2 = (char)(seconds2 + NUMERIC_VALUES_TO_ASCII);

            appendTo.Append(minutesASCII1).Append(minutesASCII2).Append(COLON).Append(secondsASCII1).Append(secondsASCII2);
        }

        /// <summary>
        /// Checks if an string is valid to be converted into C# compiled code. In order to pass this check, the string must follow the following rules:<br/>
        /// 1. It does not start with a digit {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}.<br/>
        /// 2. It is not null nor empty.<br/>
        /// 3. It does not contain any white spaces { }.<br/>
        /// 4. It is not any C# reserved keywords (Check the array field called <see cref="cSharpReservedKeywords"/>)<br/>
        /// 5. It does not contain any Unicode Non Spacing Mark character https://www.fileformat.info/info/unicode/category/Mn/list.htm<br/>
        /// 6. It does not contain any Unicode Enclosing Mark character https://www.fileformat.info/info/unicode/category/Me/list.htm<br/>
        /// </summary>
        /// <param name="stringToValidate">The string to validate</param>
        /// <returns></returns>
        public static bool IsStringValidForCSharpNamingConventions(string stringToValidate)
        {
            //Check if the string is null, empty or full of white spaces.
            if (string.IsNullOrWhiteSpace(stringToValidate))
            {
                return false;
            }

            //Check if the string starts with a digit number.
            if (char.IsDigit(stringToValidate[0]))
            {
                return false;
            }

            //Check if the string contains any white spaces.
            if(stringToValidate.Any(char.IsWhiteSpace))
            {
                return false;
            }

            //Check if the string is a C# reserved keyword
            if(Array.Exists(cSharpReservedKeywords, keyword => keyword == stringToValidate))
            {
                return false;
            }

            //Check if the string contains any Unicode Non Spacing or Enclosing Marks such as accents and other uncommon symbols.
            string normalized = stringToValidate.Normalize(NormalizationForm.FormD);
            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// REMINDER: wrap in using to get cleanup when going out of scope
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static Stream GenerateStreamFromString(string @string)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@string);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
