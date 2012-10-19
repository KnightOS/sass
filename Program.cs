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
            var assembler = new Assembler(z80);
            // TODO: Command line arguments
            string file = File.ReadAllText(args[0]);
            var output = assembler.Assemble(file, "foo.asm");
            File.WriteAllBytes(args[1], output.Data);
            var errors = from l in output.Listing
                         where l.Warning != AssemblyWarning.None || l.Error != AssemblyError.None
                         orderby l.RootLineNumber
                         select l;
            foreach (var listing in errors)
            {
                if (listing.Error != AssemblyError.None)
                    Console.WriteLine(listing.FileName + " [" + listing.LineNumber + "]: Error: " + listing.Error);
                if (listing.Warning != AssemblyWarning.None)
                    Console.WriteLine(listing.FileName + " [" + listing.LineNumber + "]: Warning: " + listing.Warning);
            }
        }

        public static Stream LoadResource(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }
    }
}
