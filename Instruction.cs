using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Instruction
    {
        internal Instruction()
        {
        }

        internal Instruction(string match, string value)
        {
            Match = match;
            Value = value;
            ImmediateValues = new Dictionary<char, ImmediateValue>();
            Operands = new Dictionary<char, Operand>();
        }

        internal string Match { get; set; }
        public string Value { get; set; }
        // key, bits
        public Dictionary<char, ImmediateValue> ImmediateValues { get; set; }
        // key, operand
        public Dictionary<char, Operand> Operands { get; set; }
    }
}
