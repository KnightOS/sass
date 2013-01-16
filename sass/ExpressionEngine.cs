using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace sass
{
    public class ExpressionEngine
    {
        public Dictionary<string, uint> Equates { get; set; }
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
            Equates = new Dictionary<string, uint>();
        }

        public ulong Evaluate(string expression, uint PC)
        {
            expression = expression.Trim();
            // Check for parenthesis
            if (HasOperators(expression))
            {
                // Recurse
                var parts = SplitExpression(expression);
                if (parts[0] == "" && parts[1] == "-") // Negate
                    return (ulong)-(long)Evaluate(parts[2], PC);
                if (parts[0] == "" && parts[1] == "~") // NOT
                    return ~Evaluate(parts[2], PC);
                switch (parts[1]) // Evaluate
                {
                    case "*":
                        return Evaluate(parts[0], PC)
                               *
                               Evaluate(parts[2], PC);
                    case "/":
                        return Evaluate(parts[0], PC)
                               /
                               Evaluate(parts[2], PC);
                    case "%":
                        return Evaluate(parts[0], PC)
                               %
                               Evaluate(parts[2], PC);
                    case "+":
                        return Evaluate(parts[0], PC)
                               +
                               Evaluate(parts[2], PC);
                    case "<<":
                        return Evaluate(parts[0], PC)
                               <<
                               (int)Evaluate(parts[2], PC);
                    case ">>":
                        return Evaluate(parts[0], PC)
                               >>
                               (int)Evaluate(parts[2], PC);
                    case "<":
                        return Evaluate(parts[0], PC)
                               <
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case "<=":
                        return Evaluate(parts[0], PC)
                               <=
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case ">":
                        return Evaluate(parts[0], PC)
                               >
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case ">=":
                        return Evaluate(parts[0], PC)
                               >=
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case "==":
                        return Evaluate(parts[0], PC)
                               ==
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case "!=":
                        return Evaluate(parts[0], PC)
                               !=
                               Evaluate(parts[2], PC) ? 1UL : 0UL;
                    case "&":
                        return Evaluate(parts[0], PC)
                               &
                               Evaluate(parts[2], PC);
                    case "^":
                        return Evaluate(parts[0], PC)
                               ^
                               Evaluate(parts[2], PC);
                    case "|":
                        return Evaluate(parts[0], PC)
                               |
                               Evaluate(parts[2], PC);
                    case "&&":
                        return (Evaluate(parts[0], PC) == 1
                               &&
                               Evaluate(parts[2], PC) == 1) ? 1UL : 0UL;
                    case "||":
                        return (Evaluate(parts[0], PC) == 1
                               ||
                               Evaluate(parts[2], PC) == 1) ? 1UL : 0UL;
                }
            }
            else
            {
                // Interpret value
                if (expression == "$")
                    return PC;
                else if (expression.StartsWith("0x") || expression.StartsWith("$") || expression.EndsWith("h")) // Hex
                    return Convert.ToUInt64(expression, 16);
                else if (expression.StartsWith("0b") || expression.StartsWith("%") || expression.EndsWith("b")) // Binary
                    return Convert.ToUInt64(expression, 2);
                else if (expression.StartsWith("0o")) // Octal
                    return Convert.ToUInt64(expression, 8);
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
                        if (Equates.ContainsKey(expression.ToLower()))
                            return Equates[expression.ToLower()];
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
