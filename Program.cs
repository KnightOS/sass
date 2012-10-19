using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace sass
{
    public class Program
    {
        static void Main(string[] args)
        {
            InstructionSet z80;
            using (var stream = new StreamReader(LoadResource("sass.z80.table")))
                z80 = InstructionSet.Load(stream.ReadToEnd());
            Assembler assembler = new Assembler(z80);
            // TODO: Command line arguments
            string file = File.ReadAllText(args[0]);
            var output = assembler.Assemble(file);
        }

        public static Stream LoadResource(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }
    }
}
