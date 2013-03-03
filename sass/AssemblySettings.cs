using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class AssemblySettings
    {
        public AssemblySettings()
        {
            // Some values intentionally redundantly set for clarity
            Verbose = false;
            ListingOutput = null;
            IncludePath = new string[0];
            SymbolOutput = null;
        }

        public bool Verbose { get; set; }
        public string ListingOutput { get; set; }
        public string[] IncludePath { get; set; }
        public string SymbolOutput { get; set; }
    }
}
