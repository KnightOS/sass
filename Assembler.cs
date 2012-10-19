using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Assembler
    {
        public InstructionSet InstructionSet { get; set; }
        public ExpressionEngine ExpressionEngine { get; set; }

        public Assembler(InstructionSet instructionSet)
        {
            InstructionSet = instructionSet;
            ExpressionEngine = new ExpressionEngine();
        }

        public AssemblyOutput Assemble(string assembly)
        {
            var output = new AssemblyOutput();
            assembly = assembly.Replace("\r", "");
            uint PC = 0;
            string[] lines = assembly.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().TrimComments();
                if (line.StartsWith(".") || line.StartsWith("#")) // Directive
                {
                }
                else if (line.StartsWith(":") || line.EndsWith(":")) // Label
                {
                }
                else
                {
                    // Check for macro
                    // TODO
                    // Instruction
                    var match = InstructionSet.Match(line);
                    if (match == null)
                    {
                        // Unknown instruction
                        output.Listing.Add(new Listing
                        {
                            Code = line,
                            CodeType = CodeType.Instruction,
                            Error = AssemblyError.InvalidInstruction,
                            Warning = AssemblyWarning.None,
                            Instruction = match,
                            Address = PC
                        });
                    }
                    else
                    {
                        // Instruction to be fully assembled in the next pass
                        output.Listing.Add(new Listing
                        {
                            Code = line,
                            CodeType = CodeType.Instruction,
                            Error = AssemblyError.None,
                            Warning = AssemblyWarning.None,
                            Instruction = match,
                            Address = PC
                        });
                        PC += match.Length;
                    }
                }
            }
            return Finish(output);
        }

        private AssemblyOutput Finish(AssemblyOutput output)
        {
            List<byte> finalBinary = new List<byte>();
            for (int i = 0; i < output.Listing.Count; i++)
            {
                var entry = output.Listing[i];
                if (entry.CodeType == CodeType.Instruction)
                {
                    // Assemble output string
                    string instruction = entry.Instruction.Value.ToLower();
                    foreach (var operand in entry.Instruction.Operands)
                        instruction = instruction.Replace("@" + operand.Key, operand.Value.Value);
                    foreach (var value in entry.Instruction.ImmediateValues)
                    {
                        // TODO: Truncation warning
                        if (value.Value.RelativeToPC)
                            instruction = instruction.Replace("^" + value.Key, ConvertToBinary(
                                entry.Address -
                                (ExpressionEngine.Evaluate(value.Value.Value, entry.Address) + entry.Instruction.Length),
                                value.Value.Bits));
                        else
                            instruction = instruction.Replace("%" + value.Key, ConvertToBinary(
                                ExpressionEngine.Evaluate(value.Value.Value, entry.Address),
                                value.Value.Bits));
                    }
                    entry.Output = ExpressionEngine.ConvertFromBinary(instruction);
                    finalBinary.AddRange(entry.Output);
                }
            }
            return output;
        }

        private static string ConvertToBinary(ulong value, int bits) // Little endian octets
        {
            ulong mask = 1;
            string result = "";
            for (int i = 0; i < bits; i++)
            {
                if ((value & mask) == mask)
                    result = "1" + result;
                else
                    result = "0" + result;
                mask <<= 1;
            }
            // Convert to little endian
            string little = "";
            for (int i = 0; i < result.Length; i += 8)
                little = result.Substring(i, 8) + little;
            return little;
        }
    }
}
