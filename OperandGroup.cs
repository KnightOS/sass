using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class OperandGroup
    {
        public OperandGroup(string name)
        {
            Name = name;
            Operands = new List<Operand>();
        }

        public string Name { get; set; }
        public List<Operand> Operands { get; set; }
    }
}
