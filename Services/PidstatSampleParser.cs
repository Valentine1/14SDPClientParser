using System.Globalization;
using _13StatParser.Models;

namespace _13StatParser.Services
{
    /// <summary>
    /// One-pass line processor: sample headers <c>=== Sample N</c>, then pidstat rows.
    /// No regex — index-based scanning after the time column.
    /// </summary>
    public sealed class PidstatSampleParser
    {
        private const string SamplePrefix = "=== Sample ";

        public Dictionary<int, Dictionary<string, ThreadCpuEntry>> Samples { get; } = new();

        public void Reset()
        {
            Samples.Clear();
            _currentSampleKey = null;
            useTGID = false;
            prevTGID = false;
        }

        private int? _currentSampleKey;
        private bool useTGID;
        private bool prevTGID;
        private const int startLookTGID = 17;
        private const int startLookTID = 27;
        private const int startLookCpuLoad = 69;
        private const int startLookName = 83;
        private const int startLookCpuNumber = 80;

        public void ProcessLine(string line)
        {
            if (prevTGID)
            {
                prevTGID = false;
                return;
            }
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (line.StartsWith(SamplePrefix, StringComparison.Ordinal))
            {
                if (TryParseSampleNumber(line, out int sampleNo))
                    _currentSampleKey = sampleNo;
                useTGID = true;
                return;
            }

            if (_currentSampleKey is null)
                return;

            if (line.StartsWith("Linux ", StringComparison.Ordinal))
                return;

            if (line.StartsWith("Average:", StringComparison.OrdinalIgnoreCase))
                return;

            if (IsPidstatTableHeaderLine(line))
                return;
            int Tid = GetThreadNumber(line);
            if (useTGID)
            {
                useTGID = false;
                prevTGID = true;
            }

            if (Tid == 0)
            {
                throw new Exception($"Problem getting TID at {_currentSampleKey}");
            }

            int sampleKey = _currentSampleKey.Value;
            if (!Samples.TryGetValue(sampleKey, out Dictionary<string, ThreadCpuEntry>? byTid))
            {
                byTid = new Dictionary<string, ThreadCpuEntry>();
                Samples[sampleKey] = byTid;
            }

            string tidKey = Tid.ToString(CultureInfo.InvariantCulture);
            if (byTid.ContainsKey(tidKey))
                return;

            float cpuLoad = GetCPULoad(line);
            if (cpuLoad == -1)
            {
                throw new Exception($"Problem getting CPU% at {_currentSampleKey}");
            }

            float cpuNumber = GetCpuNumber(line);
            if (cpuNumber == -1)
            {
                throw new Exception($"Problem getting CPU number at {_currentSampleKey}");
            }

            string threadName = GetThreadName(line);
            if (string.IsNullOrWhiteSpace(threadName))
            {
                throw new Exception($"Problem getting thread name at sample {sampleKey}, TID {Tid}");
            }

            byTid[tidKey] = new ThreadCpuEntry
            {
                CpuPercent = cpuLoad,
                CpuNumber = cpuNumber,
                ThreadName = threadName
            };
        }

        private string GetThreadName(string line)
        {
            int start = startLookName;
            while (line[start] == ' ')
            {
                start++;
            }

            ReadOnlySpan<char> tail = line.AsSpan(start).TrimEnd();
            return tail.ToString().Replace("|__", string.Empty, StringComparison.Ordinal);
        }

        private float GetCPULoad(string line)
        {
            float cpuLoad = -1;
            int start = startLookCpuLoad;
            while (!char.IsDigit(line[start]))
            {
                start++;
            }
            int end = start;
            while (char.IsDigit(line[end]) || char.IsPunctuation(line[end]))
            {
                end++;
            }
            float.TryParse(line.AsSpan(start, end - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out cpuLoad);
            return cpuLoad;
        }

        private float GetCpuNumber(string line)
        {
            float cpuNumber = -1;
            int start = startLookCpuNumber;
            while (!char.IsDigit(line[start]))
            {
                start++;
            }
            int end = start;
            while (char.IsDigit(line[end]) || char.IsPunctuation(line[end]))
            {
                end++;
            }
            float.TryParse(line.AsSpan(start, end - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out cpuNumber);
            return cpuNumber;
        }

        private int GetThreadNumber(string line)
        {
            int tid = 0;
            int start = useTGID ? startLookTGID : startLookTID;
            while (!char.IsDigit(line[start]))
            {
                start++;
            }
            int end = start;
            while (char.IsDigit(line[end]))
            {
                end++;
            }
            int.TryParse(line.AsSpan(start, end - start), NumberStyles.None, CultureInfo.InvariantCulture, out tid);
            return tid;
        }

        private static bool TryParseSampleNumber(string line, out int sampleNumber)
        {
            sampleNumber = 0;
            int i = SamplePrefix.Length;
            int start = i;
            while (i < line.Length && char.IsDigit(line[i]))
                i++;
            if (i == start)
                return false;
            return int.TryParse(line.AsSpan(start, i - start), NumberStyles.None, CultureInfo.InvariantCulture, out sampleNumber);
        }

        private static bool IsPidstatTableHeaderLine(string line)
        {
            return line.Contains(" UID ", StringComparison.Ordinal);
        }
    }
}
