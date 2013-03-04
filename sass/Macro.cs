using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Macro
    {
        internal Macro()
        {
            Parameters = new string[0];
        }

        public Macro(string name, string[] parameters, string code)
        {
            Name = name;
            Parameters = parameters;
            Code = code;
        }

        public string Name { get; set; }
        public string[] Parameters { get; set; }
        public string Code { get; set; }
        public bool AddEntry { get; set; }
    }
}
