namespace _13StatParser.Models
{
    public sealed class TopicTextSummaryEntry
    {
        public string Topic { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Deliver { get; set; }
        public double Uncompress { get; set; }
        public double Render { get; set; }
        public double TotalAver { get; set; }
        public double AbsoluteMax { get; set; }
    }
}
