using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Operand
    {
        public Operand(string name, string value)
        {
            Name = name;
            Value = value;
            foreach (var c in value)
            {
                if (c != '0' && c != '1')
                    throw new ArgumentException("Value must be a binary number", "value");
            }
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
