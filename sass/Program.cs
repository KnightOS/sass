using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace sass
{
    public class Program
    {
        public static Dictionary<string, InstructionSet> InstructionSets;

        public static int Main(string[] args)
        {
            Console.WriteLine("SirCmpwn's Assembler     Copyright Drew DeVault 2013");

            InstructionSets = new Dictionary<string, InstructionSet>();
            InstructionSets.Add("z80", LoadInternalSet("sass.Tables.z80.table"));
            string instructionSet = "z80"; // Default
            string inputFile = null, outputFile = null;
            var settings = new AssemblySettings();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    try
                    {
                        switch (arg)
                        {
                            case "--input":
                            case "--input-file":
                                inputFile = args[++i];
                                break;
                            case "--output":
                            case "--output-file":
                                outputFile = args[++i];
                                break;
                            case "-l":
                            case "--listing":
                                settings.ListingOutput = args[++i];
                                break;
                            case "--inc":
                            case "--include":
                                settings.IncludePath = args[++i].Split(';');
                                break;
                            case "--instr":
                            case "--instruction-set":
                                instructionSet = args[++i];
                                break;
                            case "-h":
                            case "-?":
                            case "/?":
                            case "/help":
                            case "-help":
                            case "--help":
                                DisplayHelp();
                                return 0;
                            case "-v":
                            case "--verbose":
                                settings.Verbose = true;
                                break;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Error: Invalid usage. Use sass.exe --help for usage information.");
                        return 1;
                    }
                }
                else
                {
                    if (inputFile == null)
                        inputFile = args[i];
                    else if (outputFile == null)
                        outputFile = args[i];
                    else
                    {
                        Console.WriteLine("Error: Invalid usage. Use sass.exe --help for usage information.");
                        return 1;
                    }
                }
            }

            if (inputFile == null)
            {
                Console.WriteLine("No input file specified. Use sass.exe --help for usage information.");
                return 1;
            }
            if (outputFile == null)
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + ".bin";

            InstructionSet selectedInstructionSet;
            if (!InstructionSets.ContainsKey(instructionSet))
            {
                if (File.Exists(instructionSet))
                    selectedInstructionSet = InstructionSet.Load(File.ReadAllText(instructionSet));
                else
                {
                    Console.WriteLine("Specified instruction set was not found.");
                    return 1;
                }
            }
            else
                selectedInstructionSet = InstructionSets[instructionSet];

            var assembler = new Assembler(selectedInstructionSet, settings);
            string file = File.ReadAllText(inputFile);
            var watch = new Stopwatch();
            watch.Start();
            var output = assembler.Assemble(file, inputFile);
            watch.Stop();

            File.WriteAllBytes(outputFile, output.Data);
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

            if (settings.Verbose || settings.ListingOutput != null)
            {
                var listing = GenerateListing(output);
                if (settings.Verbose)
                    Console.Write(listing);
                if (settings.ListingOutput != null)
                    File.WriteAllText(settings.ListingOutput, listing);
            }

            Console.WriteLine("Assembly done: {0} ms", watch.ElapsedMilliseconds);
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            return errors.Count();
        }

        public static string GenerateListing(AssemblyOutput output)
        {
            // I know this can be optimized, I might optmize it eventually
            int maxLineNumber = output.Listing.Max(l => l.LineNumber).ToString().Length;
            int maxFileLength = output.Listing.Max(l => l.FileName.Length);
            int maxBinaryLength = output.Listing.Max(l =>
                {
                    if (l.Output == null || l.Output.Length == 0)
                        return 0;
                    return l.Output.Length * 3 - 1;
                });
            int addressLength = output.InstructionSet.WordSize / 4 + 2;
            string formatString = "{0,-" + maxFileLength + "}/{1,-" + maxLineNumber + "} ({2}): {3,-" + maxBinaryLength + "}  {4}" + Environment.NewLine;
            string addressFormatString = "X" + addressLength;
            // Listing format looks something like this:
            // file.asm/1 (0x1234): DE AD BE EF    ld a, 0xBEEF
            // file.asm/2 (0x1236):              label:
            // file.asm/3 (0x1236):              #directive
            var builder = new StringBuilder();
            string file, address, binary, code;
            int line;
            foreach (var entry in output.Listing)
            {
                file = entry.FileName;
                line = entry.LineNumber;
                address = "0x" + entry.Address.ToString(addressFormatString);
                code = entry.Code;
                if (entry.Output != null && entry.Output.Length != 0)
                {
                    binary = string.Empty;
                    for (int i = 0; i < entry.Output.Length; i++)
                        binary += entry.Output[i].ToString("X2") + " ";
                    binary = binary.Remove(binary.Length - 1);
                    code = "  " + code;
                }
                else
                    binary = string.Empty;
                builder.AppendFormat(formatString, file, line, address, binary, code);
            }
            return builder.ToString();
        }

        public static Stream LoadResource(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }

        private static InstructionSet LoadInternalSet(string name)
        {
            InstructionSet set;
            using (var stream = new StreamReader(LoadResource(name)))
                set = InstructionSet.Load(stream.ReadToEnd());
            return set;
        }

        private static void DisplayHelp()
        {
            // TODO
        }
    }
}
