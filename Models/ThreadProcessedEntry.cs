using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _13StatParser.Models
{
    public class ThreadProcessedEntry
    {
        public string Key { get; set; } = string.Empty;
        public int OriginalOrder { get; set; }
        public bool Selected { get; set; } = false;
        public bool IsMain { get; set; } = false;
        public double CpuPercentSum { get; set; }
        public double CpuPercentAver { get; set; }
        public double CpuPercentMax { get; set; }
        public int Count { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
