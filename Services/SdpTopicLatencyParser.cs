using System.Globalization;

namespace _13StatParser.Services
{
    public sealed class TopicMinuteAggregate
    {
        public double FirstMs { get; set; }
        public double SecondMs { get; set; }
        public double ThirdMs { get; set; }
        public int SampleCount { get; set; }
        public double TotalMs => FirstMs + SecondMs + ThirdMs;
    }

    public sealed class ParseResult
    {
        public SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> MinuteTopicAggregates { get; init; } =
            new();
        public Dictionary<string, double> TopicMaxSampleTotals { get; init; } = new(StringComparer.Ordinal);
        public int TotalLines { get; init; }
    }

    internal sealed class PendingArrival
    {
        public string TopicKey { get; init; } = string.Empty;
        public DateTime ArrivedTimestamp { get; init; }
        public DateTime MinuteBucket { get; init; }
        public double FirstMs { get; init; }
    }

    public sealed class SdpTopicLatencyParser
    {
        private const string RequiredTopicPart = "price_update_sdp_agg";
        private const string ArrivedMarker = "****TOPIC ARRIVED:";
        private const string ProcessedMarker = "processed solace topic time by UI ";
        private const string TimeSentMarker = "TimeSent:";
        private const string IdMarker = "id:";
        private const string TotalMarker = "total:";
        private const string TopicMarker = "topic:";

        public ParseResult ParseFile(string path, Action<int, string>? reportProgress, Func<bool>? isCancellationRequested)
        {
            var minuteTopic = new SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>>();
            var topicMaxSamples = new Dictionary<string, double>(StringComparer.Ordinal);
            var pendingArrivalsById = new Dictionary<string, PendingArrival>(StringComparer.Ordinal);

            int totalLines = 0;
            long lastProgressPercent = -1;
            var fileInfo = new FileInfo(path);
            long fileLength = fileInfo.Exists ? Math.Max(1L, fileInfo.Length) : 1L;

            using var fs = File.OpenRead(path);
            using var reader = new StreamReader(fs);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (isCancellationRequested?.Invoke() == true)
                    return new ParseResult
                    {
                        MinuteTopicAggregates = minuteTopic,
                        TopicMaxSampleTotals = topicMaxSamples,
                        TotalLines = totalLines
                    };

                totalLines++;

                if (line.Length < 24)
                    continue;

                if (!TryParseLogTimestamp(line.AsSpan(0, 23), out DateTime logTimestamp))
                    continue;

                if (line.Contains(ArrivedMarker, StringComparison.Ordinal))
                {
                    ParseArrivedLine(
                        line,
                        logTimestamp,
                        pendingArrivalsById,
                        minuteTopic);
                }
                else if (line.Contains(ProcessedMarker, StringComparison.Ordinal))
                {
                    ParseProcessedLine(
                        line,
                        logTimestamp,
                        pendingArrivalsById,
                        minuteTopic,
                        topicMaxSamples);
                }

                if (totalLines % 200 == 0)
                {
                    int progressPercent = (int)Math.Min(100L, (100L * fs.Position) / fileLength);
                    if (progressPercent != lastProgressPercent)
                    {
                        lastProgressPercent = progressPercent;
                        reportProgress?.Invoke(progressPercent, $"{totalLines:N0} lines");
                    }
                }
            }

            foreach (PendingArrival pending in pendingArrivalsById.Values)
                UpdateMaxSample(topicMaxSamples, pending.TopicKey, pending.FirstMs);

            AddAllTopicsAggregates(minuteTopic, topicMaxSamples);

            reportProgress?.Invoke(100, $"{totalLines:N0} lines");
            return new ParseResult
            {
                MinuteTopicAggregates = minuteTopic,
                TopicMaxSampleTotals = topicMaxSamples,
                TotalLines = totalLines
            };
        }

        private static void ParseArrivedLine(
            string line,
            DateTime arrivedTimestamp,
            Dictionary<string, PendingArrival> pendingArrivalsById,
            SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> minuteTopic)
        {
            int arrivedIndex = line.IndexOf(ArrivedMarker, StringComparison.Ordinal);
            if (arrivedIndex < 0)
                return;

            int timeSentIndex = line.IndexOf(TimeSentMarker, arrivedIndex, StringComparison.Ordinal);
            if (timeSentIndex < 0)
                return;

            string topic = line[(arrivedIndex + ArrivedMarker.Length)..timeSentIndex].Trim();
            if (!topic.Contains(RequiredTopicPart, StringComparison.Ordinal))
                return;

            string topicKey = GetTopicKey(topic);
            if (string.IsNullOrWhiteSpace(topicKey))
                return;

            int timeSentValueStart = timeSentIndex + TimeSentMarker.Length;
            int timeSentValueEnd = line.IndexOf(',', timeSentValueStart);
            if (timeSentValueEnd <= timeSentValueStart)
                return;

            string timeSentValue = line.Substring(timeSentValueStart, timeSentValueEnd - timeSentValueStart).Trim();
            if (!TryParseLogTimestamp(timeSentValue.AsSpan(), out DateTime timeSent))
                return;

            string? id = ExtractIdValue(line);
            if (string.IsNullOrWhiteSpace(id))
                return;

            double firstMs = (arrivedTimestamp - timeSent).TotalMilliseconds;
            DateTime minuteBucket = TruncateToMinute(arrivedTimestamp);
            AddAggregate(minuteTopic, minuteBucket, topicKey, firstMs, 0, 0, 1);

            pendingArrivalsById[id] = new PendingArrival
            {
                TopicKey = topicKey,
                ArrivedTimestamp = arrivedTimestamp,
                MinuteBucket = minuteBucket,
                FirstMs = firstMs
            };
        }

        private static void ParseProcessedLine(
            string line,
            DateTime processedTimestamp,
            Dictionary<string, PendingArrival> pendingArrivalsById,
            SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> minuteTopic,
            Dictionary<string, double> topicMaxSamples)
        {
            int markerIndex = line.IndexOf(ProcessedMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return;

            int idStart = markerIndex + ProcessedMarker.Length;
            int idEnd = line.IndexOf(',', idStart);
            if (idEnd <= idStart)
                return;

            string id = line[idStart..idEnd].Trim();
            if (!pendingArrivalsById.TryGetValue(id, out PendingArrival? pending))
                return;

            int totalIndex = line.IndexOf(TotalMarker, idEnd, StringComparison.Ordinal);
            if (totalIndex < 0)
                return;

            int totalStart = totalIndex + TotalMarker.Length;
            int totalEnd = line.IndexOf(" ms", totalStart, StringComparison.Ordinal);
            if (totalEnd <= totalStart)
                return;

            string totalValue = line.Substring(totalStart, totalEnd - totalStart).Trim();
            if (!double.TryParse(totalValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double totalMs))
                return;

            int topicIndex = line.IndexOf(TopicMarker, totalEnd, StringComparison.Ordinal);
            if (topicIndex < 0)
                return;

            string topic = line[(topicIndex + TopicMarker.Length)..].Trim();
            if (!topic.Contains(RequiredTopicPart, StringComparison.Ordinal))
                return;

            double secondMs = (processedTimestamp - pending.ArrivedTimestamp).TotalMilliseconds - totalMs;
            AddAggregate(minuteTopic, pending.MinuteBucket, pending.TopicKey, 0, secondMs, totalMs, 0);
            UpdateMaxSample(topicMaxSamples, pending.TopicKey, pending.FirstMs + secondMs + totalMs);

            pendingArrivalsById.Remove(id);
        }

        private static string? ExtractIdValue(string line)
        {
            int idIndex = line.IndexOf(IdMarker, StringComparison.Ordinal);
            if (idIndex < 0)
                return null;
            string id = line[(idIndex + IdMarker.Length)..].Trim();
            return id.Length == 0 ? null : id;
        }

        private static void AddAllTopicsAggregates(
            SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> minuteTopic,
            Dictionary<string, double> topicMaxSamples)
        {
            const string allTopics = "All Topics";
            foreach ((DateTime minute, Dictionary<string, TopicMinuteAggregate> topics) in minuteTopic)
            {
                double first = 0;
                double second = 0;
                double third = 0;
                int topicCount = 0;

                foreach ((string topic, TopicMinuteAggregate aggregate) in topics)
                {
                    if (string.Equals(topic, allTopics, StringComparison.Ordinal))
                        continue;
                    double divisor = aggregate.SampleCount > 0 ? aggregate.SampleCount : 1;
                    first += aggregate.FirstMs / divisor;
                    second += aggregate.SecondMs / divisor;
                    third += aggregate.ThirdMs / divisor;
                    topicCount++;
                }

                if (topicCount == 0)
                    continue;

                topics[allTopics] = new TopicMinuteAggregate
                {
                    FirstMs = first / topicCount,
                    SecondMs = second / topicCount,
                    ThirdMs = third / topicCount,
                    SampleCount = 1
                };
            }

            double allTopicsMax = 0;
            foreach ((string topic, double maxSample) in topicMaxSamples)
            {
                if (string.Equals(topic, allTopics, StringComparison.Ordinal))
                    continue;
                if (maxSample > allTopicsMax)
                    allTopicsMax = maxSample;
            }
            topicMaxSamples[allTopics] = allTopicsMax;
        }

        private static void AddAggregate(
            SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> minuteTopic,
            DateTime minute,
            string topicKey,
            double firstMs,
            double secondMs,
            double thirdMs,
            int sampleCountIncrement)
        {
            if (!minuteTopic.TryGetValue(minute, out Dictionary<string, TopicMinuteAggregate>? byTopic))
            {
                byTopic = new Dictionary<string, TopicMinuteAggregate>(StringComparer.Ordinal);
                minuteTopic[minute] = byTopic;
            }

            if (!byTopic.TryGetValue(topicKey, out TopicMinuteAggregate? aggregate))
            {
                aggregate = new TopicMinuteAggregate();
                byTopic[topicKey] = aggregate;
            }

            aggregate.FirstMs += firstMs;
            aggregate.SecondMs += secondMs;
            aggregate.ThirdMs += thirdMs;
            aggregate.SampleCount += sampleCountIncrement;
        }

        private static void UpdateMaxSample(Dictionary<string, double> topicMaxSamples, string topicKey, double value)
        {
            if (topicMaxSamples.TryGetValue(topicKey, out double current))
                topicMaxSamples[topicKey] = Math.Max(current, value);
            else
                topicMaxSamples[topicKey] = value;
        }

        private static string GetTopicKey(string topic)
        {
            int idx = topic.LastIndexOf('/');
            return idx >= 0 && idx < topic.Length - 1 ? topic[(idx + 1)..].Trim() : topic.Trim();
        }

        private static DateTime TruncateToMinute(DateTime ts)
            => new(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, ts.Kind);

        private static bool TryParseLogTimestamp(ReadOnlySpan<char> value, out DateTime timestamp)
            => DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out timestamp);
    }
}
