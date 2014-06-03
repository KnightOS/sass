using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace sass
{
    public class Assembler
    {
        public InstructionSet InstructionSet { get; set; }
        public ExpressionEngine ExpressionEngine { get; set; }
        public AssemblyOutput Output { get; set; }
        public Encoding Encoding { get; set; }
        public List<string> IncludePaths { get; set; }
        public List<Macro> Macros { get; set; }
        public AssemblySettings Settings { get; set; }

        private uint PC { get; set; }
        private string[] Lines { get; set; }
        private int RootLineNumber { get; set; }
        private Stack<int> LineNumbers { get; set; }
        private Stack<string> FileNames { get; set; }
        private Stack<bool> IfStack { get; set; }
        private Stack<bool> WorkingIfStack { get; set; }
        private int SuspendedLines { get; set; }
        private int CurrentIndex { get; set; }
        private string CurrentLine { get; set; }
        private bool Listing { get; set; }

        public Assembler(InstructionSet instructionSet, AssemblySettings settings)
        {
            InstructionSet = instructionSet;
            Settings = settings;
            ExpressionEngine = new ExpressionEngine(settings);
            SuspendedLines = 0;
            LineNumbers = new Stack<int>();
            FileNames = new Stack<string>();
            IncludePaths = new List<string>();
            Macros = new List<Macro>();
            IfStack = new Stack<bool>();
            WorkingIfStack = new Stack<bool>();
            Listing = true;
        }

        readonly string[] ifDirectives = new[] { "endif", "else", "elif", "elseif", "ifdef", "ifndef", "if" };

        public AssemblyOutput Assemble(string assembly, string fileName = null)
        {
            Output = new AssemblyOutput();
            Output.InstructionSet = InstructionSet;
            assembly = assembly.Replace("\r", "");
            PC = 0;
            Lines = assembly.Split('\n');
            FileNames.Push(Path.GetFileName(fileName));
            LineNumbers.Push(0);
            RootLineNumber = 0;
            IfStack.Push(true);
            for (CurrentIndex = 0; CurrentIndex < Lines.Length; CurrentIndex++)
            {
                CurrentLine = Lines[CurrentIndex].Trim().TrimComments();
                if (SuspendedLines == 0)
                {
                    LineNumbers.Push(LineNumbers.Pop() + 1);
                    RootLineNumber++;
                }
                else
                    SuspendedLines--;

                if (!IfStack.Peek())
                {
                    bool match = false;
                    if (CurrentLine.StartsWith("#") || CurrentLine.StartsWith("."))
                    {
                        var directive = CurrentLine.Substring(1);
                        if (CurrentLine.Contains((' ')))
                            directive = directive.Remove(CurrentLine.IndexOf((' '))).Trim();
                        if (ifDirectives.Contains(directive.ToLower()))
                            match = true;
                    }
                    if (!match)
                        continue;
                }

                if (CurrentLine.SafeContains(".equ") && !CurrentLine.StartsWith(".equ"))
                {
                    var name = CurrentLine.Remove(CurrentLine.SafeIndexOf(".equ"));
                    var definition = CurrentLine.Substring(CurrentLine.SafeIndexOf(".equ") + 4);
                    CurrentLine = ".equ " + name.Trim() + " " + definition.Trim();
                }

                // Check for macro
                if (!CurrentLine.StartsWith(".macro") && !CurrentLine.StartsWith("#macro") && !CurrentLine.StartsWith(".undefine") && !CurrentLine.StartsWith("#undefine"))
                {
                    Macro macroMatch = null;
                    string[] parameters = null;
                    string parameterDefinition = null;
                    foreach (var macro in Macros)
                    {
                        if (CurrentLine.ToLower().SafeContains(macro.Name))
                        {
                            // Try to match
                            int startIndex = CurrentLine.ToLower().SafeIndexOf(macro.Name);
                            int endIndex = startIndex + macro.Name.Length - 1;
                            if (macro.Parameters.Length != 0)
                            {
                                if (endIndex + 1 >= CurrentLine.Length)
                                    continue;
                                if (CurrentLine.Length < endIndex + 1 || CurrentLine[endIndex + 1] != '(')
                                    continue;
                                if (macroMatch != null && macro.Name.Length < macroMatch.Name.Length)
                                    continue;
                                parameterDefinition = CurrentLine.Substring(endIndex + 2, CurrentLine.LastIndexOf(')') - (endIndex + 2));
                                parameters = parameterDefinition.SafeSplit(',');
                                if (parameters.Length != macro.Parameters.Length)
                                    continue;
                                // Matched
                                macroMatch = macro;
                            }
                            else
                                macroMatch = macro;
                        }
                    }
                    if (macroMatch != null)
                    {
                        // Add an entry to the listing
                        AddOutput(CodeType.Directive);
                        var code = macroMatch.Code;
                        int index = 0;
                        foreach (var parameter in macroMatch.Parameters)
                            code = code.Replace(parameter.Trim(), parameters[index++].Trim());
                        string newLine;
                        if (parameterDefinition != null)
                            newLine = CurrentLine.Replace(macroMatch.Name + "(" + parameterDefinition + ")", code, StringComparison.InvariantCultureIgnoreCase);
                        else
                        {
                            if (CurrentLine.Substring(CurrentLine.ToLower().IndexOf(macroMatch.Name) + macroMatch.Name.Length).StartsWith("()"))
                                newLine = CurrentLine.Replace(macroMatch.Name + "()", code, StringComparison.InvariantCultureIgnoreCase);
                            else
                                newLine = CurrentLine.Replace(macroMatch.Name, code, StringComparison.InvariantCultureIgnoreCase);
                        }
                        var newLines = newLine.Replace("\r\n", "\n").Split('\n');
                        SuspendedLines += newLines.Length;
                        // Insert macro
                        Lines = Lines.Take(CurrentIndex).Concat(newLines).Concat(Lines.Skip(CurrentIndex + 1)).ToArray();
                        CurrentIndex--;
                        continue;
                    }
                }

                // Find same-line labels
                if (CurrentLine.Contains(":"))
                {
                    int length = 0;
                    bool isLabel = true;
                    for (int j = 0; j < CurrentLine.Length; j++)
                    {
                        if (char.IsLetterOrDigit(CurrentLine[j]) || CurrentLine[j] == '_')
                            length++;
                        else if (CurrentLine[j] == ':')
                            break;
                        else
                        {
                            isLabel = false;
                            break;
                        }
                    }
                    if (isLabel)
                    {
                        var label = CurrentLine.Remove(length).ToLower();
                        label = label.ToLower();
                        if (label == "_")
                        {
                            // Relative
                            ExpressionEngine.RelativeLabels.Add(new RelativeLabel
                            {
                                Address = PC,
                                RootLineNumber = RootLineNumber
                            });
                            AddOutput(CodeType.Label);
                        }
                        else
                        {
                            bool local = label.StartsWith(".");
                            if (local)
                                label = label.Substring(1) + "@" + ExpressionEngine.LastGlobalLabel;
                            bool valid = true;
                            for (int k = 0; k < label.Length; k++) // Validate label
                            {
                                if (!char.IsLetterOrDigit(label[k]) && label[k] != '_')
                                {
                                    if (local && label[k] == '@')
                                        continue;
                                    valid = false;
                                    break;
                                }
                            }
                            if (!valid)
                                AddError(CodeType.Label, AssemblyError.InvalidLabel);
                            else if (ExpressionEngine.Symbols.ContainsKey(label.ToLower()))
                                AddError(CodeType.Label, AssemblyError.DuplicateName);
                            else
                            {
                                AddOutput(CodeType.Label);
                                ExpressionEngine.Symbols.Add(label.ToLower(), new Symbol(PC, true));
                                if (!local)
                                    ExpressionEngine.LastGlobalLabel = label.ToLower();
                            }
                        }
                        CurrentLine = CurrentLine.Substring(length + 1).Trim();
                    }
                }

                if (CurrentLine.StartsWith(":") || CurrentLine.EndsWith(":")) // Label
                {
                    string label;
                    if (CurrentLine.StartsWith(":"))
                        label = CurrentLine.Substring(1).Trim();
                    else
                        label = CurrentLine.Remove(CurrentLine.Length - 1).Trim();
                    label = label.ToLower();
                    if (label == "_")
                    {
                        // Relative
                        ExpressionEngine.RelativeLabels.Add(new RelativeLabel
                        {
                            Address = PC,
                            RootLineNumber = RootLineNumber
                        });
                        AddOutput(CodeType.Label);
                    }
                    else
                    {
                        bool local = label.StartsWith(".");
                        if (local)
                            label = label.Substring(1) + "@" + ExpressionEngine.LastGlobalLabel;
                        bool valid = true;
                        for (int k = 0; k < label.Length; k++) // Validate label
                        {
                            if (!char.IsLetterOrDigit(label[k]) && label[k] != '_')
                            {
                                if (local && label[k] == '@')
                                    continue;
                                valid = false;
                                break;
                            }
                        }
                        if (!valid)
                            AddError(CodeType.Label, AssemblyError.InvalidLabel);
                        else if (ExpressionEngine.Symbols.ContainsKey(label.ToLower()))
                            AddError(CodeType.Label, AssemblyError.DuplicateName);
                        else
                        {
                            AddOutput(CodeType.Label);
                            ExpressionEngine.Symbols.Add(label.ToLower(), new Symbol(PC, true));
                            if (!local)
                                ExpressionEngine.LastGlobalLabel = label.ToLower();
                        }
                    }
                    continue;
                }

                if (CurrentLine.SafeContains('\\'))
                {
                    // Split lines up
                    var split = CurrentLine.SafeSplit('\\');
                    Lines = Lines.Take(CurrentIndex).Concat(split).
                        Concat(Lines.Skip(CurrentIndex + 1)).ToArray();
                    SuspendedLines = split.Length;
                    CurrentIndex--;
                    continue;
                }

                if (CurrentLine.StartsWith(".") || CurrentLine.StartsWith("#")) // Directive
                {
                    // Some directives need to be handled higher up
                    var directive = CurrentLine.Substring(1).Trim();
                    string[] parameters = new string[0];
                    if (directive.SafeIndexOf(' ') != -1)
                        parameters = directive.Substring(directive.SafeIndexOf(' ')).Trim().SafeSplit(' ');
                    if (directive.ToLower().StartsWith("macro"))
                    {
                        var definitionLine = CurrentLine; // Used to update the listing later
                        if (parameters.Length == 0)
                        {
                            AddError(CodeType.Directive, AssemblyError.InvalidDirective);
                            continue;
                        }
                        string definition = directive.Substring(directive.SafeIndexOf(' ')).Trim();
                        var macro = new Macro();
                        if (definition.Contains("("))
                        {
                            var parameterDefinition = definition.Substring(definition.SafeIndexOf('(') + 1);
                            parameterDefinition = parameterDefinition.Remove(parameterDefinition.SafeIndexOf(')'));
                            // NOTE: This probably introduces the ability to use ".macro foo(bar)this_doesnt_cause_errors"
                            if (string.IsNullOrEmpty(parameterDefinition))
                                macro.Parameters = new string[0];
                            else
                                macro.Parameters = parameterDefinition.SafeSplit(',');
                            macro.Name = definition.Remove(definition.SafeIndexOf('(')).ToLower();
                        }
                        else
                            macro.Name = definition.ToLower(); // TODO: Consider enforcing character usage restrictions
                        for (CurrentIndex++; CurrentIndex < Lines.Length; CurrentIndex++)
                        {
                            CurrentLine = Lines[CurrentIndex].Trim().TrimComments();
                            LineNumbers.Push(LineNumbers.Pop() + 1);
                            RootLineNumber++;
                            if (CurrentLine == ".endmacro" || CurrentLine == "#endmacro")
                                break;
                            macro.Code += CurrentLine + Environment.NewLine;
                        }
                        macro.Code = macro.Code.Remove(macro.Code.Length - Environment.NewLine.Length);
                        macro.Name = macro.Name.ToLower();
                        if (Macros.Any(m => m.Name == macro.Name && m.Parameters.Length == macro.Parameters.Length))
                        {
                            AddError(CodeType.Label, AssemblyError.DuplicateName);
                            continue;
                        }
                        Macros.Add(macro);
                        // Add an entry to the listing
                        Output.Listing.Add(new Listing
                        {
                            Code = definitionLine,
                            CodeType = CodeType.Directive,
                            Error = AssemblyError.None,
                            Warning = AssemblyWarning.None,
                            Address = PC,
                            FileName = FileNames.Peek(),
                            LineNumber = LineNumbers.Peek(),
                            RootLineNumber = RootLineNumber
                        });
                    }
                    else
                    {
                        var result = HandleDirective(CurrentLine);
                        if (result != null)
                            Output.Listing.Add(result);
                    }
                    continue;
                }
                else
                {
                    if (string.IsNullOrEmpty(CurrentLine) || !Listing)
                        continue;
                    // Check instructions
                    var match = InstructionSet.Match(CurrentLine);
                    if (match == null)
                        AddError(CodeType.Instruction, AssemblyError.InvalidInstruction); // Unknown instruction
                    else
                    {
                        // Instruction to be fully assembled in the next pass
                        Output.Listing.Add(new Listing
                        {
                            Code = CurrentLine,
                            CodeType = CodeType.Instruction,
                            Error = AssemblyError.None,
                            Warning = AssemblyWarning.None,
                            Instruction = match,
                            Address = PC,
                            FileName = FileNames.Peek(),
                            LineNumber = LineNumbers.Peek(),
                            RootLineNumber = RootLineNumber
                        });
                        PC += match.Length;
                    }
                }
            }
            return Finish(Output);
        }

        private void AddOutput(CodeType type)
        {
            Output.Listing.Add(new Listing
            {
                Code = CurrentLine,
                CodeType = type,
                Error = AssemblyError.None,
                Warning = AssemblyWarning.None,
                Address = PC,
                FileName = FileNames.Peek(),
                LineNumber = LineNumbers.Peek(),
                RootLineNumber = RootLineNumber
            });
        }

        private void AddError(CodeType type, AssemblyError error)
        {
            Output.Listing.Add(new Listing
            {
                Code = CurrentLine,
                CodeType = type,
                Error = error,
                Warning = AssemblyWarning.None,
                Address = PC,
                FileName = FileNames.Peek(),
                LineNumber = LineNumbers.Peek(),
                RootLineNumber = RootLineNumber
            });
        }

        private AssemblyOutput Finish(AssemblyOutput output)
        {
            var finalBinary = new List<byte>();
            ExpressionEngine.LastGlobalLabel = null;
            for (int i = 0; i < output.Listing.Count; i++)
            {
                var entry = output.Listing[i];
                RootLineNumber = entry.RootLineNumber;
                PC = entry.Address;
                LineNumbers = new Stack<int>(new[] { entry.LineNumber });
                if (entry.CodeType == CodeType.Directive)
                {
                    if (entry.PostponeEvalulation)
                        output.Listing[i] = HandleDirective(entry.Code, true);
                    if (output.Listing[i].Output != null)
                        finalBinary.AddRange(output.Listing[i].Output);
                    continue;
                }
                if (entry.Error != AssemblyError.None)
                    continue;
                if (entry.CodeType == CodeType.Label)
                {
                    var name = entry.Code.Remove(entry.Code.IndexOf(':')).Trim(':').ToLower();
                    if (!name.StartsWith(".") && name != "_")
                        ExpressionEngine.LastGlobalLabel = name;
                }
                else if (entry.CodeType == CodeType.Instruction)
                {
                    // Assemble output string
                    string instruction = entry.Instruction.Value.ToLower();
                    foreach (var operand in entry.Instruction.Operands)
                        instruction = instruction.Replace("@" + operand.Key, operand.Value.Value);
                    foreach (var value in entry.Instruction.ImmediateValues)
                    {
                        try
                        {
                            bool truncated;
                            if (value.Value.RelativeToPC)
							{
								var exp = ExpressionEngine.Evaluate(value.Value.Value, entry.Address, entry.RootLineNumber);
                                instruction = instruction.Replace("^" + value.Key, ConvertToBinary(
									(exp - entry.Instruction.Length) - entry.Address,
									value.Value.Bits, true, out truncated));
							}
                            else if (value.Value.RstOnly)
                            {
                                truncated = false;
                                var rst = (byte)ExpressionEngine.Evaluate(value.Value.Value, entry.Address, entry.RootLineNumber);
                                if ((rst & ~0x7) != rst || rst > 0x38)
                                    entry.Error = AssemblyError.InvalidExpression;
                                else
                                {
                                    instruction = instruction.Replace("&" + value.Key,
										ConvertToBinary((ulong)rst >> 3, 3, false, out truncated));
                                }
                            }
                            else
                                instruction = instruction.Replace("%" + value.Key, ConvertToBinary(
                                    ExpressionEngine.Evaluate(value.Value.Value, entry.Address, entry.RootLineNumber),
									value.Value.Bits, false, out truncated));
                            if (truncated)
                                entry.Warning = AssemblyWarning.ValueTruncated;
                        }
                        catch (KeyNotFoundException)
                        {
                            entry.Error = AssemblyError.UnknownSymbol;
                        }
                        catch (InvalidOperationException)
                        {
                            entry.Error = AssemblyError.InvalidExpression;
                        }
                    }
                    if (entry.Error == AssemblyError.None)
                    {
                        entry.Output = ExpressionEngine.ConvertFromBinary(instruction);
                        finalBinary.AddRange(entry.Output);
                    }
                    else
                        finalBinary.AddRange(new byte[entry.Instruction.Length]);
                }
            }
            output.Data = finalBinary.ToArray();
            return output;
        }

		private static string ConvertToBinary(ulong value, int bits, bool signed, out bool truncated) // Little endian octets
        {
            ulong mask = 1;
            string result = "";
            ulong truncationMask = 1;
            for (int i = 0; i < bits; i++)
            {
                truncationMask <<= 1;
                truncationMask |= 1;
                if ((value & mask) == mask)
                    result = "1" + result;
                else
                    result = "0" + result;
                mask <<= 1;
            }
			truncationMask >>= 1;
			if (signed)
				truncationMask >>= 1;
			truncated = (value & truncationMask) != value;
            // Convert to little endian
            if (result.Length % 8 != 0)
                return result;
            string little = "";
            for (int i = 0; i < result.Length; i += 8)
                little = result.Substring(i, 8) + little;
            return little;
        }

        #region Directives

        private Listing HandleDirective(string line, bool passTwo = false)
        {
            string directive = line.Substring(1).Trim();
            string[] parameters = new string[0];
            string parameter = "";
            if (directive.SafeContains(' '))
            {
                parameter = directive.Substring(directive.SafeIndexOf(' ') + 1);
                parameters = parameter.SafeSplit(' ');
                directive = directive.Remove(directive.SafeIndexOf(' '));
            }
            directive = directive.ToLower();
            var listing = new Listing
            {
                Code = line,
                CodeType = CodeType.Directive,
                Address = PC,
                Error = AssemblyError.None,
                Warning = AssemblyWarning.None,
                FileName = FileNames.Peek(),
                LineNumber = LineNumbers.Peek(),
                RootLineNumber = RootLineNumber
            };
            try
            {
                switch (directive)
                {
                    case "block":
                        {
                            ulong amount = ExpressionEngine.Evaluate(parameter, PC, RootLineNumber);
                            listing.Output = new byte[amount];
                            PC += (uint)amount;
                            return listing;
                        }
                    case "byte":
                    case "db":
                        {
                            if (passTwo)
                            {
                                var result = new List<byte>();
                                parameters = parameter.SafeSplit(',');
                                foreach (var p in parameters)
                                {
                                    if (p.Trim().StartsWith("\"") && p.Trim().EndsWith("\""))
                                        result.AddRange(
											Settings.Encoding.GetBytes(p.Trim().Substring(1, p.Trim().Length - 2).Unescape()));
                                    else
                                    {
                                        try
                                        {
                                            result.Add((byte)ExpressionEngine.Evaluate(p, PC++, RootLineNumber));
                                        }
                                        catch (KeyNotFoundException)
                                        {
                                            listing.Error = AssemblyError.UnknownSymbol;
                                        }
                                    }
                                }
                                listing.Output = result.ToArray();
                                return listing;
                            }
                            else
                            {
                                parameters = parameter.SafeSplit(',');
                                int length = 0;
                                foreach (var p in parameters)
                                {
                                    if (p.StartsWith("\"") && p.EndsWith("\""))
                                        length += p.Substring(1, p.Length - 2).Unescape().Length;
                                    else
                                        length++;
                                }
                                listing.Output = new byte[length];
                                listing.PostponeEvalulation = true;
                                PC += (uint)listing.Output.Length;
                                return listing;
                            }
                        }
                    case "word":
                    case "dw":
                        {
                            if (passTwo)
                            {
                                var result = new List<byte>();
                                parameters = parameter.SafeSplit(',');
                                foreach (var item in parameters)
                                    result.AddRange(TruncateWord(ExpressionEngine.Evaluate(item, PC++, RootLineNumber)));
                                listing.Output = result.ToArray();
                                return listing;
                            }
                            else
                            {
                                listing.Output = new byte[parameters.Length * (InstructionSet.WordSize / 8)];
                                listing.PostponeEvalulation = true;
                                PC += (uint)listing.Output.Length;
                                return listing;
                            }
                        }
                    case "error":
                    case "echo":
                        {
                            if (passTwo)
                            {
                                string output = "";
                                bool formatOutput = false;
                                List<object> formatParameters = new List<object>();
                                foreach (var item in parameters)
                                {
                                    if (item.Trim().StartsWith("\"") && item.EndsWith("\""))
                                    {
                                        output += item.Substring(1, item.Length - 2);
                                        formatOutput = true;
                                    }
                                    else
                                    {
                                        if (!formatOutput)
                                            output += ExpressionEngine.Evaluate(item, PC, RootLineNumber);
                                        else
                                        {
                                            formatParameters.Add(ExpressionEngine.Evaluate(item, PC, RootLineNumber));
                                        }
                                    }
                                }
                                if (formatOutput)
                                    output = string.Format(output, formatParameters.ToArray());
                                Console.WriteLine((directive == "error" ? "User Error: " : "") + output);
                                return listing;
                            }
                            else
                            {
                                listing.PostponeEvalulation = true;
                                return listing;
                            }
                        }
                        break;
                    case "end":
                        return listing;
                    case "fill":
                        {
                            parameters = parameter.SafeSplit(',');
                            ulong amount = ExpressionEngine.Evaluate(parameters[0], PC, RootLineNumber);
                            if (parameters.Length == 1)
                            {
                                Array.Resize<string>(ref parameters, 2);
                                parameters[1] = "0";
                            }
                            listing.Output = new byte[amount];
                            for (int i = 0; i < (int)amount; i++)
                                listing.Output[i] = (byte)ExpressionEngine.Evaluate(parameters[1], PC++, RootLineNumber);
                            return listing;
                        }
                    case "org":
                        PC = (uint)ExpressionEngine.Evaluate(parameter, PC, RootLineNumber);
                        return listing;
                    case "include":
                        {
                            string file = GetIncludeFile(parameter);
                            if (file == null)
                            {
                                listing.Error = AssemblyError.FileNotFound;
                                return listing;
                            }
                            FileNames.Push(Path.GetFileName(parameter.Substring(1, parameter.Length - 2)));
                            LineNumbers.Push(0);
                            string includedFile = File.ReadAllText(file) + "\n.endfile";
                            string[] lines = includedFile.Replace("\r", "").Split('\n');
                            Lines =
                                Lines.Take(CurrentIndex + 1)
                                     .Concat(lines)
                                     .Concat(Lines.Skip(CurrentIndex + 1))
                                     .ToArray();
                            return listing;
                        }
                    case "endfile": // Special, undocumented directive
                        RootLineNumber--;
                        LineNumbers.Pop();
                        FileNames.Pop();
                        return null;
                    case "equ":
                        if (parameters.Length == 1)
                        {
                            if (ExpressionEngine.Symbols.ContainsKey(parameters[0].ToLower()))
                            {
                                listing.Error = AssemblyError.DuplicateName;
                                return listing;
                            }
                            ExpressionEngine.Symbols.Add(parameters[0].ToLower(), new Symbol(1));
                        }
                        else
                        {
                            if (ExpressionEngine.Symbols.ContainsKey(parameters[0].ToLower()))
                            {
                                listing.Error = AssemblyError.DuplicateName;
                                return listing;
                            }
                            ExpressionEngine.Symbols.Add(parameters[0].ToLower(), new Symbol(
                                                                                      (uint)
                                                                                      ExpressionEngine.Evaluate(
                                                                                          parameter.Substring(
                                                                                              parameter.IndexOf(' ') + 1)
                                                                                                   .Trim(), PC,
                                                                                          RootLineNumber)));
                        }
                        return listing;
                    case "exec":
                        if (parameters.Length == 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        else
                        {
                            var process = new ProcessStartInfo(parameters[0], string.Join(" ", parameters.Skip(1).ToArray()));
                            process.UseShellExecute = false;
                            process.RedirectStandardOutput = true;
                            var p = Process.Start(process);
                            var output = p.StandardOutput.ReadToEnd().Trim('\n', '\r', ' ');
                            p.WaitForExit();
                            listing.Output = Settings.Encoding.GetBytes(output);
                            PC += (uint)listing.Output.Length;
                            return listing;
                        }
                    case "define":
                        if (parameters.Length == 1)
                        {
                            if (ExpressionEngine.Symbols.ContainsKey(parameters[0].ToLower()))
                            {
                                listing.Error = AssemblyError.DuplicateName;
                                return listing;
                            }
                            ExpressionEngine.Symbols.Add(parameters[0].ToLower(), new Symbol(1));
                        }
                        else
                        {
                            var macro = new Macro();
                            if (parameter.Contains("("))
                            {
                                var parameterDefinition = parameter.Substring(parameter.SafeIndexOf('(') + 1);
                                parameterDefinition = parameterDefinition.Remove(parameterDefinition.SafeIndexOf(')'));
                                // NOTE: This probably introduces the ability to use ".macro foo(bar)this_doesnt_cause_errors"
                                macro.Parameters = parameterDefinition.SafeSplit(',');
                                for (int i = 0; i < macro.Parameters.Length; i++)
                                    macro.Parameters[i] = macro.Parameters[i].Trim();
                                macro.Name = parameter.Remove(parameter.SafeIndexOf('('));
                                macro.Code = parameter.Substring(parameter.SafeIndexOf(')') + 1);
                            }
                            else
                            {
                                macro.Name = parameter.Remove(parameter.SafeIndexOf(' ') + 1);
                                // TODO: Consider enforcing character usage restrictions
                                macro.Code = parameter.Substring(parameter.SafeIndexOf(' ') + 1).Trim();
                            }
                            macro.Name = macro.Name.ToLower().Trim();
                            if (Macros.Any(m => m.Name == macro.Name && m.Parameters.Length == macro.Parameters.Length))
                            {
                                listing.Error = AssemblyError.DuplicateName;
                                return listing;
                            }
                            Macros.Add(macro);
                        }
                        return listing;
                    case "if":
                        if (parameters.Length == 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        if (!IfStack.Peek())
                        {
                            WorkingIfStack.Push(false);
                            return listing;
                        }
                        try
                        {
                            IfStack.Push(ExpressionEngine.Evaluate(parameter, PC, RootLineNumber) != 0);
                        }
                        catch (InvalidOperationException)
                        {
                            listing.Error = AssemblyError.InvalidExpression;
                        }
                        catch (KeyNotFoundException)
                        {
                            listing.Error = AssemblyError.UnknownSymbol;
                        }
                        return listing;
                    case "ifdef":
                        {
                            if (parameters.Length != 1)
                            {
                                listing.Error = AssemblyError.InvalidDirective;
                                return listing;
                            }
                            if (!IfStack.Peek())
                            {
                                WorkingIfStack.Push(false);
                                return listing;
                            }
                            var result = ExpressionEngine.Symbols.ContainsKey(parameters[0].ToLower());
                            if (!result)
                                result = Macros.Any(m => m.Name.ToLower() == parameters[0].ToLower());
                            IfStack.Push(result);
                            return listing;
                        }
                    case "ifndef":
                        {
                            if (parameters.Length != 1)
                            {
                                listing.Error = AssemblyError.InvalidDirective;
                                return listing;
                            }
                            if (!IfStack.Peek())
                            {
                                WorkingIfStack.Push(false);
                                return listing;
                            }
                            var result = ExpressionEngine.Symbols.ContainsKey(parameters[0].ToLower());
                            if (!result)
                                result = Macros.Any(m => m.Name.ToLower() == parameters[0].ToLower());
                            IfStack.Push(!result);
                            return listing;
                        }
                    case "endif":
                        if (parameters.Length != 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        if (IfStack.Count == 1)
                        {
                            listing.Error = AssemblyError.UncoupledStatement;
                            return listing;
                        }
                        if (WorkingIfStack.Any())
                        {
                            WorkingIfStack.Pop();
                            return listing;
                        }
                        IfStack.Pop();
                        return listing;
                    case "else":
                        if (parameters.Length != 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        if (WorkingIfStack.Any())
                            return listing;
                        IfStack.Push(!IfStack.Pop());
                        return listing;
                    //case "elif": // TODO: Requires major logic changes
                    //case "elseif":
                    //    if (IfStack.Peek())
                    //    {
                    //        IfStack.Pop();
                    //        IfStack.Push(false);
                    //        return listing;
                    //    }
                    //    return listing;
                    case "ascii":
                        if (parameters.Length == 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        if (!(parameter.StartsWith("\"") && parameter.EndsWith("\"")))
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        parameter = parameter.Substring(1, parameter.Length - 2);
                        listing.Output = Settings.Encoding.GetBytes(parameter.Unescape());
                        return listing;
                    case "asciiz":
						if (passTwo)
						{
							var result = new List<byte>();
							parameters = parameter.SafeSplit(',');
							foreach (var p in parameters)
							{
								if (p.Trim().StartsWith("\"") && p.Trim().EndsWith("\""))
									result.AddRange(Settings.Encoding.GetBytes(p.Trim().Substring(1, p.Length - 2).Unescape()));
								else
									listing.Error = AssemblyError.InvalidDirective;
							}
							listing.Output = new byte[result.Count + 1];
							Array.Copy(result.ToArray(), listing.Output, result.Count);
							listing.Output[listing.Output.Length - 1] = 0;
							return listing;
						}
						else
						{
							parameters = parameter.SafeSplit(',');
							int length = 0;
							foreach (var p in parameters)
							{
								if (p.StartsWith("\"") && p.EndsWith("\""))
									length += p.Substring(1, p.Length - 2).Unescape().Length;
								else
									listing.Error = AssemblyError.InvalidDirective;
							}
							length++;
							listing.Output = new byte[length];
							listing.PostponeEvalulation = true;
							PC += (uint)listing.Output.Length;
							return listing;
						}
						break;
                    case "asciip":
						if (passTwo)
						{
							var result = new List<byte>();
							parameters = parameter.SafeSplit(',');
							foreach (var p in parameters)
							{
								if (p.Trim().StartsWith("\"") && p.Trim().EndsWith("\""))
									result.AddRange(Settings.Encoding.GetBytes(p.Trim().Substring(1, p.Length - 2).Unescape()));
								else
									listing.Error = AssemblyError.InvalidDirective;
							}
							listing.Output = new byte[result.Count + 1];
							listing.Output[0] = (byte)result.Count;
							Array.Copy(result.ToArray(), 0, listing.Output, 1, result.Count);
							return listing;
						}
						else
						{
							parameters = parameter.SafeSplit(',');
							int length = 0;
							foreach (var p in parameters)
							{
								if (p.StartsWith("\"") && p.EndsWith("\""))
									length += p.Substring(1, p.Length - 2).Unescape().Length;
								else
									listing.Error = AssemblyError.InvalidDirective;
							}
							length++;
							listing.Output = new byte[length];
							listing.PostponeEvalulation = true;
							PC += (uint)listing.Output.Length;
							return listing;
						}
						break;
                    case "nolist":
                        Listing = false;
                        return listing;
                    case "list":
                        Listing = true;
                        return listing;
                    case "undefine":
                        if (parameters.Length == 0)
                        {
                            listing.Error = AssemblyError.InvalidDirective;
                            return listing;
                        }
                        foreach (var item in parameters)
                        {
                            if (Macros.Any(m => m.Name == item.ToLower()))
                                Macros.Remove(Macros.FirstOrDefault(m => m.Name == item));
                            else if (ExpressionEngine.Symbols.ContainsKey(item.ToLower()))
                                ExpressionEngine.Symbols.Remove(item.ToLower());
                            else
                            {
                                listing.Error = AssemblyError.UnknownSymbol;
                                return listing;
                            }
                        }
                        return listing;
                }
            }
            catch (KeyNotFoundException)
            {
                listing.Error = AssemblyError.UnknownSymbol;
                return listing;
            }
            catch (InvalidOperationException)
            {
                listing.Error = AssemblyError.InvalidExpression;
                return listing;
            }
            return null;
        }

        private string GetIncludeFile(string file)
        {
            file = file.Substring(1, file.Length - 2); // Remove <> or ""
            if (File.Exists(file))
                return file;
            foreach (var path in Settings.IncludePath)
            {
                if (File.Exists(Path.Combine(path, file)))
                    return Path.Combine(path, file);
            }
            return null;
        }

        private byte[] TruncateWord(ulong value)
        {
            var array = BitConverter.GetBytes(value);
            return array.Take(InstructionSet.WordSize / 8).ToArray();
        }

        #endregion
    }
}