using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class InstructionSet
    {
        public Dictionary<string, OperandGroup> OperandGroups { get; set; }
        public List<Instruction> Instructions { get; set; }

        private InstructionSet()
        {
            OperandGroups = new Dictionary<string, OperandGroup>();
            Instructions = new List<Instruction>();
        }

        public static InstructionSet Load(string definition)
        {
            string[] lines = definition.Replace("\r", "").Split('\n');
            var table = new InstructionSet();
            for (int i = 0; i < lines.Length; i++ )
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
                    string match, value;
                    match = parts[1];
                    value = line.Substring(line.IndexOf(' ') + 1);
                    value = value.Substring(value.IndexOf(' ') + 1);
                    table.Instructions.Add(new Instruction(match, value.Replace(" ", "")));
                }
            }
            return table;
        }
    }
}
