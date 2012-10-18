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
    }
}
