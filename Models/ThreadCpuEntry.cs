namespace _13StatParser.Models
{
    /// <summary>Per-thread row within one sample: total %CPU and command name (|__ stripped).</summary>
    public sealed class ThreadCpuEntry
    {
        public double CpuPercent { get; init; }
        public double CpuNumber { get; init; }
        public string ThreadName { get; init; } = string.Empty;
    }
}
