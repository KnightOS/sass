using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Instruction
    {
        public Instruction(string match, string value)
        {
            Match = match;
            Value = value;
        }

        public string Match { get; set; }
        public string Value { get; set; }
    }
}
