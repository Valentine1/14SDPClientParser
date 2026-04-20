namespace _13StatParser.Models
{
    public class TimeSample
    {
        public int Second { get; set; }
        public DateTime Timestamp { get; set; }
        // Key format: "TID:Name" to uniquely identify threads
        public Dictionary<string, double> ThreadCpuPercent { get; set; } = new Dictionary<string, double>();
    }
}

