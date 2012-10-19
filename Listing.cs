using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class Listing
    {
        public CodeType CodeType { get; set; }
        public string Code { get; set; }
        public Instruction Instruction { get; set; }
        public AssemblyError Error { get; set; }
        public AssemblyWarning Warning { get; set; }
        public uint Address { get; set; }
        public byte[] Output { get; set; }
        public int LineNumber { get; set; }
        public int RootLineNumber { get; set; }
        public string FileName { get; set; }
    }
}
