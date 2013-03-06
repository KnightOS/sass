using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class ImmediateValue
    {
        public ImmediateValue()
        {
            RelativeToPC = false;
        }

        public int Bits { get; set; }
        public string Value { get; set; }
        public bool RelativeToPC { get; set; }
        public bool RstOnly { get; set; }
    }
}
