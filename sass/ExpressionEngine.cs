using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace sass
{
    public class ExpressionEngine
    {
        public Dictionary<string, Symbol> Symbols { get; set; }
        public List<RelativeLabel> RelativeLabels { get; set; }
        public string LastGlobalLabel { get; set; }
        // Grouped by priority, based on C operator precedence
        public static string[][] Operators = new[]
            {
                new[] { "*", "/", "%" },
                new[] { "+", "-" },
                new[] { "<<", ">>" },
                new[] { "<", "<=", ">", ">=" },
                new[] { "==", "!=" },
                new[] { "&" },
                new[] { "^" },
                new[] { "|" },
                new[] { "&&" },
                new[] { "||" }
            };

        public ExpressionEngine()
        {
            Symbols = new Dictionary<string, Symbol>();
            RelativeLabels = new List<RelativeLabel>();
        }

        public ulong Evaluate(string expression, uint PC, int rootLineNumber)
        {
            expression = expression.Trim();
            // Check for relative labels (special case, because they're bloody annoying to parse)
            if (expression.EndsWith("_"))
            {
                bool relative = true;
                int offset = 0;
                for (int i = 0; i < expression.Length - 1; i++)
                {
                    if (expression[i] == '-')
                        offset--;
                    else if (expression[i] == '+')
                        offset++;
                    else
                    {
                        relative = false;
                        break;
                    }
                }
                if (offset < 0)
                    offset++;
                if (relative)
                {
                    int i;
                    for (i = 0; i < RelativeLabels.Count; i++)
                    {
                        if (RelativeLabels[i].RootLineNumber > rootLineNumber)
                            break;
                    }
                    i += offset;
                    if (i < 0 || i >= RelativeLabels.Count)
                        throw new KeyNotFoundException("Relative label not found.");
                    return RelativeLabels[i].Address;
                }
            }
            // Check for parenthesis
            if (HasOperators(expression))
            {
                // Recurse
                var parts = SplitExpression(expression);
                if (parts[0] == "" && parts[1] == "-") // Negate
                    return (ulong)-(long)Evaluate(parts[2], PC, rootLineNumber);
                if (parts[0] == "" && parts[1] == "~") // NOT
                    return ~Evaluate(parts[2], PC, rootLineNumber);
                if (parts[0] == string.Empty && parts[1] == "%")
                    return Convert.ToUInt64(expression.Trim('%', 'b'), 2);
                switch (parts[1]) // Evaluate
                {
                    case "+":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               +
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "-":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               -
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "*":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               *
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "/":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               /
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "%":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               %
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "<<":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               <<
                               (int)Evaluate(parts[2], PC, rootLineNumber);
                    case ">>":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               >>
                               (int)Evaluate(parts[2], PC, rootLineNumber);
                    case "<":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               <
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case "<=":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               <=
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case ">":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               >
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case ">=":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               >=
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case "==":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               ==
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case "!=":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               !=
                               Evaluate(parts[2], PC, rootLineNumber) ? 1UL : 0UL;
                    case "&":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               &
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "^":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               ^
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "|":
                        return Evaluate(parts[0], PC, rootLineNumber)
                               |
                               Evaluate(parts[2], PC, rootLineNumber);
                    case "&&":
                        return (Evaluate(parts[0], PC, rootLineNumber) == 1
                               &&
                               Evaluate(parts[2], PC, rootLineNumber) == 1) ? 1UL : 0UL;
                    case "||":
                        return (Evaluate(parts[0], PC, rootLineNumber) == 1
                               ||
                               Evaluate(parts[2], PC, rootLineNumber) == 1) ? 1UL : 0UL;
                }
            }
            else
            {
                // Interpret value
                if (expression == "$")
                    return PC;
                else if (expression.StartsWith("0x")) // Hex
                    return Convert.ToUInt64(expression.Substring(2), 16);
                else if (expression.StartsWith("$") || (expression.EndsWith("h") && 
                    expression.Remove(expression.Length - 1).ToLower().Count(c => !"0123456789abcdef".Contains(c)) == 0))
                    return Convert.ToUInt64(expression.Trim('$', 'h'), 16);
                else if (expression.StartsWith("0b")) // Binary
                    return Convert.ToUInt64(expression.Substring(2), 2);
                else if (expression.StartsWith("$") || (expression.EndsWith("h") &&
                    expression.Remove(expression.Length - 1).ToLower().Count(c => !"01".Contains(c)) == 0))
                    return Convert.ToUInt64(expression.Trim('%', 'b'), 2);
                else if (expression.StartsWith("0o")) // Octal
                    return Convert.ToUInt64(expression.Substring(2), 8);
                else if (expression == "true")
                    return 1;
                else if (expression == "false")
                    return 0;
                else
                {
                    // Check for number
                    bool number = true;
                    for (int i = 0; i < expression.Length; i++)
                        if (!char.IsNumber(expression[i]))
                            number = false;
                    if (number) // Decimal
                        return Convert.ToUInt64(expression);
                    else
                    {
                        // Look up reference
                        var symbol = expression.ToLower();
                        if (symbol.StartsWith("."))
                            symbol = symbol.Substring(1) + "@" + LastGlobalLabel;
                        if (Symbols.ContainsKey(symbol))
                            return Symbols[symbol].Value;
                        throw new KeyNotFoundException("The given equate was not found.");
                    }
                }
            }
            throw new InvalidOperationException("Invalid expression");
        }

        private string[] SplitExpression(string expression)
        {
            for (int i = 0; i < Operators.Length; i++)
            {
                for (int j = 0; j < Operators[i].Length; j++)
                {
                    int index = expression.IndexOf(Operators[i][j]);
                    if (index != -1)
                    {
                        // Split along this operator
                        return new[]
                            {
                                expression.Remove(index),
                                Operators[i][j],
                                expression.Substring(index + Operators[i][j].Length)
                            };
                    }
                }
            }
            return null;
        }

        private bool HasOperators(string expression)
        {
            for (int i = 0; i < Operators.Length; i++)
            {
                for (int j = 0; j < Operators[i].Length; j++)
                {
                    if (expression.Contains(Operators[i][j]))
                        return true;
                }
            }
            return false;
        }

        public static byte[] ConvertFromBinary(string binary)
        {
            while (binary.Length % 8 != 0)
                binary = "0" + binary;
            byte[] result = new byte[binary.Length / 8];
            int i = result.Length - 1;
            while (binary.Length > 0)
            {
                string octet = binary.Substring(binary.Length - 8);
                binary = binary.Remove(binary.Length - 8);
                result[i--] = Convert.ToByte(octet, 2);
            }
            return result;
        }
    }
}
