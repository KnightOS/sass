using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class InstructionSet
    {
        public int WordSize { get; set; }

        private Dictionary<string, OperandGroup> OperandGroups { get; set; }
        private List<Instruction> Instructions { get; set; }

        private InstructionSet()
        {
            OperandGroups = new Dictionary<string, OperandGroup>();
            Instructions = new List<Instruction>();
            WordSize = 16;
        }

        public static InstructionSet Load(string definition)
        {
            string[] lines = definition.Replace("\r", "").Split('\n');
            var table = new InstructionSet();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                    continue;
                line = line.RemoveExcessWhitespace();
                if (line.StartsWith("OPERAND "))
                {
                    string[] parts = line.Split(' ');
                    var operand = new Operand(parts[2], parts[3]);
                    if (!table.OperandGroups.ContainsKey(parts[1]))
                        table.OperandGroups.Add(parts[1], new OperandGroup(parts[1]));
                    table.OperandGroups[parts[1]].Operands.Add(operand);
                }
                else if (line.StartsWith("INS "))
                {
                    string[] parts = line.Split(' ');
                    string match = parts[1];
                    string value = line.Substring(line.IndexOf(' ') + 1);
                    value = value.Substring(value.IndexOf(' ') + 1);
                    table.Instructions.Add(new Instruction(match.ToLower(), value.Replace(" ", "")));
                }
                else if (line.StartsWith("WORDSIZE "))
                    table.WordSize = int.Parse(line.Substring(9));
                else
                {
                    throw new FormatException("In the instruction set, \"" + line + "\" is not valid.");
                }
            }
            return table;
        }

        public Instruction Match(string code)
        {
            var result = new Instruction();
            foreach (var instruction in Instructions)
            {
                int j = 0;
                bool requiredWhitespaceMet = false;
                bool match = true;
                // Create new result
                result.Match = instruction.Match;
                result.Value = instruction.Value;
                result.Operands = new Dictionary<char, Operand>();
                result.ImmediateValues = new Dictionary<char, ImmediateValue>();
                for (int i = 0; i < instruction.Match.Length; i++, j++)
                {
                    if (j >= code.Length)
                    {
                        match = false;
                        break;
                    }
                    if (instruction.Match[i] == '_') // Required whitespace
                    {
                        if (requiredWhitespaceMet && char.IsWhiteSpace(code[j]))
                            i--;
                        else if (requiredWhitespaceMet && !char.IsWhiteSpace(code[j]))
                        {
                            requiredWhitespaceMet = false;
                            j--;
                        }
                        else
                        {
                            if (char.IsWhiteSpace(code[j]))
                            {
                                requiredWhitespaceMet = true;
                                i--;
                            }
                            else
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    else if (instruction.Match[i] == '-') // Optional whitespace
                    {
                        if (char.IsWhiteSpace(code[j]))
                            i--;
                        else
                            j--;
                    }
                    else if (instruction.Match[i] == '%') // Immediate value
                    {
                        char key = instruction.Match[++i]; i += 2;
                        string bitString = "";
                        while (instruction.Match[i] != '>')
                            bitString += instruction.Match[i++];
                        i++; int bits = int.Parse(bitString);
                        // Get value
                        string value = GetOperandValue(instruction, i, code, j);
                        if (value == null)
                        {
                            match = false;
                            break;
                        }
                        j += value.Length - 1;
                        result.ImmediateValues.Add(key, new ImmediateValue
                        {
                            Bits = bits,
                            Value = value
                        });
                    }
                    else if (instruction.Match[i] == '^') // Relative immediate value
                    {
                        char key = instruction.Match[++i]; i += 2;
                        string bitString = "";
                        while (instruction.Match[i] != '>')
                            bitString += instruction.Match[i++];
                        i++; int bits = int.Parse(bitString);
                        // Get value
                        string value = GetOperandValue(instruction, i, code, j);
                        if (value == null)
                        {
                            match = false;
                            break;
                        }
                        j += value.Length - 1;
                        result.ImmediateValues.Add(key, new ImmediateValue
                        {
                            Bits = bits,
                            Value = value,
                            RelativeToPC = true
                        });
                    }
                    else if (instruction.Match[i] == '&') // RST immediate value
                    {
                        char key = instruction.Match[++i];
                        i++;
                        // Get value
                        string value = GetOperandValue(instruction, i, code, j);
                        if (value == null)
                        {
                            match = false;
                            break;
                        }
                        j += value.Length - 1;
                        result.ImmediateValues.Add(key, new ImmediateValue
                        {
                            Bits = 8,
                            Value = value,
                            RstOnly = true
                        });
                    }
                    else if (instruction.Match[i] == '@')
                    {
                        char key = instruction.Match[++i]; i += 2;
                        string group = "";
                        while (instruction.Match[i] != '>')
                            group += instruction.Match[i++];
                        i++; string value = GetOperandValue(instruction, i, code, j);
                        if (value == null)
                        {
                            match = false;
                            break;
                        }
                        value = value.Trim().ToLower();
                        j += value.Length - 1;
                        var operand = GetOperand(OperandGroups[group], value);
                        if (operand == null)
                        {
                            match = false;
                            break;
                        }
                        result.Operands.Add(key, operand);
                        i--;
                    }
                    else
                    {
                        if (instruction.Match[i] != char.ToLower(code[j]))
                        {
                            match = false;
                            break;
                        }
                    }
                }
                if (j != code.Length || !match)
                    continue;
                // Match found
                return result;
            }
            return null;
        }

        private Operand GetOperand(OperandGroup operandGroup, string value)
        {
            foreach (var op in operandGroup.Operands)
            {
                if (op.Name.ToLower() == value)
                    return op;
            }
            return null;
        }

        private string GetOperandValue(Instruction instruction, int i, string code, int j)
        {
            // Get the delimiter
            char? delimiter = null;
            while (i < instruction.Match.Length)
            {
                if (instruction.Match[i] == '-' || instruction.Match[i] == '_')
                    i++;
                else
                {
                    delimiter = instruction.Match[i];
                    break;
                }
            }
            if (delimiter == null)
                return code.Substring(j);
            int index = code.SafeIndexOf(delimiter.Value, j);
            if (index == -1)
                return null;
            return code.Substring(j, index - j);
        }
    }
}
