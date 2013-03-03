using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public struct Symbol
    {
        public bool IsLabel;
        public uint Value;

        public Symbol(uint value)
        {
            Value = value;
            IsLabel = false;
        }

        public Symbol(uint value, bool isLabel)
        {
            Value = value;
            IsLabel = isLabel;
        }
    }
}
