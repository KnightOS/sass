using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Assembler
    {
        public InstructionSet InstructionSet { get; set; }

        public Assembler(InstructionSet instructionSet)
        {
            InstructionSet = instructionSet;
        }

        public AssemblyOutput Assemble(string assembly)
        {
            var output = new AssemblyOutput();
            assembly = assembly.Replace("\r", "");
            ulong PC = 0;
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

                    // Instruction
                    var match = InstructionSet.Match(line);
                    if (match == null)
                    {
                        // Unknown instruction
                        output.Listing.Add(new Listing()
                        {
                            Code = line,
                            CodeType = CodeType.Instruction,
                            Error = AssemblyError.InvalidInstruction,
                            Warning = AssemblyWarning.None,
                            Instruction = match
                        });
                    }
                    else
                    {
                        // Instruction to be fully assembled in the next pass
                        output.Listing.Add(new Listing()
                        {
                            Code = line,
                            CodeType = CodeType.Instruction,
                            Error = AssemblyError.InvalidInstruction,
                            Warning = AssemblyWarning.None,
                            Instruction = match
                        });
                    }
                }
            }
            return output;
        }
    }
}
