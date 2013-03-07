using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public static class Extensions
    {
        /// <summary>
        /// Reduces all consecutive spaces and tabs to one space.
        /// </summary>
        public static string RemoveExcessWhitespace(this string value)
        {
            string newvalue = "";
            value = value.Trim().Replace('\t', ' ');
            bool inString = false, inChar = false, previousWhitespace = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (!(char.IsWhiteSpace(value[i]) && previousWhitespace) || inString || inChar)
                    newvalue += value[i];
                if (char.IsWhiteSpace(value[i]))
                    previousWhitespace = true;
                else
                    previousWhitespace = false;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return newvalue.Trim();
        }

        /// <summary>
        /// Trims comments from the end of a line. Uses ';' as the comment
        /// delimiter.
        /// </summary>
        public static string TrimComments(this string value)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == ';' && !inString && !inChar)
                    return value.Remove(i).Trim();
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return value.Trim();
        }

        public static int SafeIndexOf(this string value, char needle, int index)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = index; i < value.Length; i++)
            {
                if (value[i] == needle && !inString && !inChar)
                    return i;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return -1;
        }

        public static int SafeIndexOf(this string value, char needle)
        {
            return SafeIndexOf(value, needle, 0);
        }

        public static int SafeIndexOf(this string value, string needle)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value.Substring(i).StartsWith(needle) && !inString && !inChar)
                    return i;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return -1;
        }

        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        /// <summary>
        /// Checks for value contained within a string, outside of '' and ""
        /// </summary>
        public static bool SafeContains(this string value, char needle)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == needle && !inString && !inChar)
                    return true;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return false;
        }

        /// <summary>
        /// Checks for value contained within a string, outside of '' and ""
        /// </summary>
        public static bool SafeContains(this string value, string needle)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value.Substring(i).StartsWith(needle) && !inString && !inChar)
                    return true;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return false;
        }

        /// <summary>
        /// Works the same as String.Split, but will not split if the requested characters are within
        /// a character or string literal.
        /// </summary>
        public static string[] SafeSplit(this string value, params char[] characters)
        {
            string[] result = new string[1];
            result[0] = "";
            bool inString = false, inChar = false;
            foreach (char c in value)
            {
                bool foundChar = false;
                if (!inString && !inChar)
                {
                    foreach (char haystack in characters)
                    {
                        if (c == haystack)
                        {
                            foundChar = true;
                            result = result.Concat(new string[] { "" }).ToArray();
                            break;
                        }
                    }
                }
                if (!foundChar)
                {
                    result[result.Length - 1] += c;
                    if (c == '"' && !inChar)
                        inString = !inString;
                    if (c == '\'' && !inString)
                        inChar = !inChar;
                }
            }
            return result;
        }

        public static string Unescape(this string value)
        {
            if (value == null)
                return null;
            string newvalue = "";
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\')
                    newvalue += value[i];
                else
                {
                    if (i + 1 == value.Length)
                        return null;
                    switch (value[i + 1])
                    {
                        case 'a':
                            newvalue += "\a";
                            break;
                        case 'b':
                            newvalue += "\b";
                            break;
                        case 'f':
                            newvalue += "\f";
                            break;
                        case 'n':
                            newvalue += "\n";
                            break;
                        case 'r':
                            newvalue += "\r";
                            break;
                        case 't':
                            newvalue += "\t";
                            break;
                        case 'v':
                            newvalue += "\v";
                            break;
                        case '\'':
                            newvalue += "\'";
                            break;
                        case '"':
                            newvalue += "\"";
                            break;
                        case '\\':
                            newvalue += "\\";
                            break;
                        case '0':
                            newvalue += "\0";
                            break;
                        case 'x':
                            if (i + 3 > value.Length)
                                return null;
                            string hex = value[i + 2].ToString() + value[i + 3].ToString();
                            i += 2;
                            try
                            {
                                newvalue += (char)Encoding.ASCII.GetBytes(new char[] { (char)Convert.ToByte(hex, 16) })[0];
                            }
                            catch
                            {
                                return null;
                            }
                            break;
                        default:
                            return null;
                    }
                    i++;
                }
            }
            return newvalue;
        }
    }
}
