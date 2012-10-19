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
            return output;
        }
    }
}
