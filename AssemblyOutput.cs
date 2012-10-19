using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sass
{
    public class AssemblyOutput
    {
        public AssemblyOutput()
        {
            Listing = new List<Listing>();
        }

        public byte[] Data { get; set; }
        public List<Listing> Listing { get; set; }
    }
}
