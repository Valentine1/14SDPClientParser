using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using _13StatParser.Models;
using _13StatParser.Services;

namespace _13StatParser
{
    public partial class Form1 : Form
    {
        private enum ThreadSortState
        {
            None,
            CpuAverAsc,
            CpuAverDesc,
            CpuMaxAsc,
            CpuMaxDesc
        }

        private BackgroundWorker parserWorker = null!;
        private BackgroundWorker processorWorker = null!;
        private readonly SdpTopicLatencyParser latencyParser = new();
        private List<ThreadProcessedEntry> processedThreads = new();
        private List<TopicTextSummaryEntry> textSummaryRows = new();
        private SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> minuteTopicAggregates = new();
        private ThreadSortState threadSortState = ThreadSortState.None;
        private readonly Dictionary<string, Color> selectedThreadColors = new();
        private int nextThreadColorIndex;
        private const string AllTopicsName = "All Topics";

        private sealed class ProcessingResult
        {
            public List<ThreadProcessedEntry> Threads { get; init; } = new();
            public List<TopicTextSummaryEntry> TextSummaryRows { get; init; } = new();
        }
        private static readonly Color[] ThreadPalette = new[]
        {
            Color.FromArgb(230, 25, 75),   // red
            Color.FromArgb(60, 180, 75),   // green
            Color.FromArgb(0, 130, 200),   // blue
            Color.FromArgb(245, 130, 48),  // orange
            Color.FromArgb(145, 30, 180),  // purple
            Color.FromArgb(70, 240, 240),  // cyan
            Color.FromArgb(240, 50, 230),  // magenta
            Color.FromArgb(210, 245, 60),  // lime
            Color.FromArgb(250, 190, 212), // pink
            Color.FromArgb(0, 128, 128),   // teal
            Color.FromArgb(170, 110, 40),  // brown
            Color.FromArgb(128, 128, 0),   // olive
            Color.FromArgb(220, 20, 60),   // crimson
            Color.FromArgb(255, 140, 0),   // dark orange
            Color.FromArgb(30, 144, 255),  // dodger blue
            Color.FromArgb(34, 139, 34),   // forest green
            Color.FromArgb(138, 43, 226),  // blue violet
            Color.FromArgb(255, 99, 71),   // tomato
            Color.FromArgb(64, 224, 208),  // turquoise
            Color.FromArgb(199, 21, 133),  // medium violet red
            Color.FromArgb(218, 165, 32),  // goldenrod
            Color.FromArgb(0, 191, 255),   // deep sky blue
            Color.FromArgb(75, 0, 130),    // indigo
            Color.FromArgb(107, 142, 35),  // olive drab
            Color.FromArgb(205, 92, 92)    // indian red
        };

        public Form1()
        {
            InitializeComponent();
            InitializeCpuFilterDropdown();
            chartCpuStacked.Dock = DockStyle.Fill;
            chartCpuStacked.ChartAreas.Clear();
            chartCpuStacked.Series.Clear();
            chartCpuStacked.Legends.Clear();
            chartCpuStacked.Resize += (_, _) => UpdateSampleChartScaleView();
            chartCpuStacked.GetToolTipText += ChartCpuStacked_GetToolTipText;
            InitializeParserWorker();
            InitializeProcessorWorker();
            FormClosing += Form1_FormClosing;
            dgvThreadsSummary.CurrentCellDirtyStateChanged += DgvThreadsSummary_CurrentCellDirtyStateChanged;
            dgvThreadsSummary.CellValueChanged += DgvThreadsSummary_CellValueChanged;
            dgvThreadsSummary.ColumnHeaderMouseClick += DgvThreadsSummary_ColumnHeaderMouseClick;
            Resize += (_, _) =>
            {
                if (lblLineCount.Visible)
                    LayoutLineCountAfterStatus();
            };
        }

        private void InitializeCpuFilterDropdown()
        {
            cmbFilterCpuThreshold.Items.Clear();
            for (int value = 0; value <= 2000; value += 100)
                cmbFilterCpuThreshold.Items.Add(value.ToString());
            cmbFilterCpuThreshold.SelectedIndex = 0;
        }

        private int GetCpuFilterThreshold()
        {
            if (cmbFilterCpuThreshold.SelectedItem is string selected &&
                int.TryParse(selected, out int threshold))
            {
                return threshold;
            }

            return 0;
        }

        private Color GetNextThreadColor()
        {
            if (nextThreadColorIndex < ThreadPalette.Length)
                return ThreadPalette[nextThreadColorIndex++];

            // Keep generating distinct colors if user exceeds predefined palette.
            int idx = nextThreadColorIndex++ - ThreadPalette.Length;
            double hue = (idx * 137.508) % 360.0;
            return ColorFromHsv(hue, 0.75, 0.92);
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = value - c;

            (double r1, double g1, double b1) = hue switch
            {
                >= 0 and < 60 => (c, x, 0d),
                >= 60 and < 120 => (x, c, 0d),
                >= 120 and < 180 => (0d, c, x),
                >= 180 and < 240 => (0d, x, c),
                >= 240 and < 300 => (x, 0d, c),
                _ => (c, 0d, x)
            };

            return Color.FromArgb(
                (int)Math.Round((r1 + m) * 255),
                (int)Math.Round((g1 + m) * 255),
                (int)Math.Round((b1 + m) * 255));
        }

        private void EnsureSelectedThreadColorsAssigned()
        {
            foreach (ThreadProcessedEntry thread in processedThreads.Where(t => t.Selected && !t.IsMain))
            {
                if (!selectedThreadColors.ContainsKey(thread.Key))
                    selectedThreadColors[thread.Key] = GetNextThreadColor();
            }
        }

        private void UpdateDisplayedSamplesLabel(int displayed, int total)
        {
            lblDisplayedSamples.Text = $"{displayed} out {total}";
        }

        private void DgvThreadsSummary_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (!dgvThreadsSummary.IsCurrentCellDirty)
                return;

            if (dgvThreadsSummary.CurrentCell is DataGridViewCheckBoxCell)
                dgvThreadsSummary.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvThreadsSummary_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!string.Equals(dgvThreadsSummary.Columns[e.ColumnIndex].Name, "colSelected", StringComparison.Ordinal))
                return;

            RefreshThreadColorColumn();
        }

        private void DgvThreadsSummary_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dgvThreadsSummary.Columns.Count || processedThreads.Count == 0)
                return;

            string columnName = dgvThreadsSummary.Columns[e.ColumnIndex].Name;
            if (!string.Equals(columnName, "colCpuAvg", StringComparison.Ordinal) &&
                !string.Equals(columnName, "colCpuMax", StringComparison.Ordinal))
                return;

            if (string.Equals(columnName, "colCpuAvg", StringComparison.Ordinal))
            {
                threadSortState = threadSortState == ThreadSortState.CpuAverAsc
                    ? ThreadSortState.CpuAverDesc
                    : ThreadSortState.CpuAverAsc;
            }
            else
            {
                threadSortState = threadSortState == ThreadSortState.CpuMaxAsc
                    ? ThreadSortState.CpuMaxDesc
                    : ThreadSortState.CpuMaxAsc;
            }

            ApplyThreadSort();
        }

        private void ApplyThreadSort()
        {
            processedThreads = threadSortState switch
            {
                ThreadSortState.CpuAverAsc => processedThreads.OrderBy(t => t.CpuPercentAver).ToList(),
                ThreadSortState.CpuAverDesc => processedThreads.OrderByDescending(t => t.CpuPercentAver).ToList(),
                ThreadSortState.CpuMaxAsc => processedThreads.OrderBy(t => t.CpuPercentMax).ToList(),
                ThreadSortState.CpuMaxDesc => processedThreads.OrderByDescending(t => t.CpuPercentMax).ToList(),
                _ => processedThreads.OrderBy(t => t.OriginalOrder).ToList()
            };

            BindThreadsSummaryGrid();
        }

        private void BindThreadsSummaryGrid()
        {
            dgvThreadsSummary.DataSource = new BindingList<ThreadProcessedEntry>(processedThreads);
            btnResetSort.Enabled = threadSortState != ThreadSortState.None;
            RefreshThreadColorColumn();

            foreach (DataGridViewColumn column in dgvThreadsSummary.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;

            if (threadSortState == ThreadSortState.CpuAverAsc)
                dgvThreadsSummary.Columns["colCpuAvg"].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
            else if (threadSortState == ThreadSortState.CpuAverDesc)
                dgvThreadsSummary.Columns["colCpuAvg"].HeaderCell.SortGlyphDirection = SortOrder.Descending;
            else if (threadSortState == ThreadSortState.CpuMaxAsc)
                dgvThreadsSummary.Columns["colCpuMax"].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
            else if (threadSortState == ThreadSortState.CpuMaxDesc)
                dgvThreadsSummary.Columns["colCpuMax"].HeaderCell.SortGlyphDirection = SortOrder.Descending;
        }

        private void BindTextSummaryGrid()
        {
            dgvTextSummary.DataSource = new BindingList<TopicTextSummaryEntry>(textSummaryRows);
        }

        private void btnResetSort_Click(object sender, EventArgs e)
        {
            if (processedThreads.Count == 0)
                return;

            threadSortState = ThreadSortState.None;
            ApplyThreadSort();
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            if (processedThreads.Count == 0)
                return;

            foreach (ThreadProcessedEntry thread in processedThreads)
                thread.Selected = false;

            BindThreadsSummaryGrid();
        }

        private void btnRebuildChart_Click(object sender, EventArgs e)
        {
            BuildSampleCpuStackChart(minuteTopicAggregates);
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (parserWorker.IsBusy)
                parserWorker.CancelAsync();
            if (processorWorker.IsBusy)
                processorWorker.CancelAsync();
        }

        private void InitializeParserWorker()
        {
            parserWorker = new BackgroundWorker();
            parserWorker.WorkerReportsProgress = true;
            parserWorker.WorkerSupportsCancellation = true;

            parserWorker.DoWork += ParserWorker_DoWork;
            parserWorker.ProgressChanged += ParserWorker_ProgressChanged;
            parserWorker.RunWorkerCompleted += ParserWorker_RunWorkerCompleted;
        }

        private void InitializeProcessorWorker()
        {
            processorWorker = new BackgroundWorker();
            processorWorker.WorkerReportsProgress = true;
            processorWorker.WorkerSupportsCancellation = true;

            processorWorker.DoWork += ProcessorWorker_DoWork;
            processorWorker.ProgressChanged += ProcessorWorker_ProgressChanged;
            processorWorker.RunWorkerCompleted += ProcessorWorker_RunWorkerCompleted;
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            string filePath = openFileDialog.FileName;
            txtFilePath.Text = filePath;

            if (parserWorker.IsBusy || processorWorker.IsBusy)
                return;

            SetParserUiRunning(true);
            progressAnalysis.Value = 0;
            lblParserStatus.Text = "Parsing...";
            ClearLineCountLabel();
            LayoutLineCountAfterStatus();
            threadSortState = ThreadSortState.None;
            btnResetSort.Enabled = false;
            btnRebuildChart.Enabled = false;
            UpdateDisplayedSamplesLabel(0, 0);
            dgvThreadsSummary.DataSource = null;
            dgvTextSummary.DataSource = null;
            processedThreads.Clear();
            textSummaryRows.Clear();
            minuteTopicAggregates.Clear();
            selectedThreadColors.Clear();
            nextThreadColorIndex = 0;
            ClearSampleCpuChart();
            parserWorker.RunWorkerAsync(filePath);
        }

        private void btnCancelAnalysis_Click(object sender, EventArgs e)
        {
            if (parserWorker.IsBusy)
                parserWorker.CancelAsync();
            else if (processorWorker.IsBusy)
                processorWorker.CancelAsync();
        }

        private void ParserWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender!;
            if (e.Argument is not string path)
                return;

            ParseResult result = latencyParser.ParseFile(
                path,
                (percent, text) => worker.ReportProgress(percent, text),
                () => worker.CancellationPending);

            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            e.Result = result;
        }

        private void ParserWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressAnalysis.Value = e.ProgressPercentage; // Math.Clamp(e.ProgressPercentage, progressAnalysis.Minimum, progressAnalysis.Maximum);
            if (e.UserState is string lineProgress)
                SetLineCountLabel(lineProgress);
        }

        private void ParserWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                SetParserUiRunning(false);
                lblParserStatus.Text = "Cancelled.";
                ClearLineCountLabel();
                progressAnalysis.Value = 0;
                return;
            }

            if (e.Error != null)
            {
                SetParserUiRunning(false);
                lblParserStatus.Text = $"Parse error: {e.Error.Message}";
                ClearLineCountLabel();
                progressAnalysis.Value = 0;
                return;
            }

            if (e.Result is not ParseResult parseResult)
            {
                SetParserUiRunning(false);
                lblParserStatus.Text = "Ready.";
                ClearLineCountLabel();
                progressAnalysis.Value = 100;
                return;
            }

            lblParserStatus.Text = "Processing...";
            ClearLineCountLabel();
            progressAnalysis.Value = 0;
            minuteTopicAggregates = parseResult.MinuteTopicAggregates;
            processorWorker.RunWorkerAsync(parseResult);
        }

        private void ProcessorWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender!;
            if (e.Argument is not ParseResult parseResult)
                return;

            int totalMinutes = parseResult.MinuteTopicAggregates.Count;
            if (totalMinutes == 0)
            {
                e.Result = new ProcessingResult();
                worker.ReportProgress(100, "0 minutes");
                return;
            }

            var topicMinuteTotals = new Dictionary<string, List<double>>(StringComparer.Ordinal);
            var topicFirstTotals = new Dictionary<string, double>(StringComparer.Ordinal);
            var topicSecondTotals = new Dictionary<string, double>(StringComparer.Ordinal);
            var topicThirdTotals = new Dictionary<string, double>(StringComparer.Ordinal);
            var topicCountTotals = new Dictionary<string, int>(StringComparer.Ordinal);
            int processed = 0;
            int lastPercent = -1;

            foreach ((DateTime _, Dictionary<string, TopicMinuteAggregate> byTopic) in parseResult.MinuteTopicAggregates)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                foreach ((string topicKey, TopicMinuteAggregate aggregate) in byTopic)
                {
                    if (!topicMinuteTotals.TryGetValue(topicKey, out List<double>? minuteTotals))
                    {
                        minuteTotals = new List<double>();
                        topicMinuteTotals[topicKey] = minuteTotals;
                    }
                    double divisor = aggregate.SampleCount > 0 ? aggregate.SampleCount : 1;
                    minuteTotals.Add(aggregate.TotalMs / divisor);

                    if (!topicFirstTotals.ContainsKey(topicKey))
                    {
                        topicFirstTotals[topicKey] = 0;
                        topicSecondTotals[topicKey] = 0;
                        topicThirdTotals[topicKey] = 0;
                        topicCountTotals[topicKey] = 0;
                    }

                    topicFirstTotals[topicKey] += aggregate.FirstMs;
                    topicSecondTotals[topicKey] += aggregate.SecondMs;
                    topicThirdTotals[topicKey] += aggregate.ThirdMs;
                    topicCountTotals[topicKey] += aggregate.SampleCount;
                }

                processed++;
                int percent = (int)((100L * processed) / totalMinutes);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    worker.ReportProgress(percent, $"{processed:N0} / {totalMinutes:N0} minutes");
                }
            }

            var result = new List<ThreadProcessedEntry>();
            IEnumerable<string> orderedTopics = topicMinuteTotals.Keys
                .OrderBy(topic => string.Equals(topic, AllTopicsName, StringComparison.Ordinal) ? 0 : 1)
                .ThenBy(topic => topic, StringComparer.Ordinal);
            int allTopicsMessageCount = topicCountTotals
                .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                .Sum(kv => kv.Value);
            double allTopicsTotalSum = topicFirstTotals
                .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                .Sum(kv => kv.Value)
                + topicSecondTotals
                .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                .Sum(kv => kv.Value)
                + topicThirdTotals
                .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                .Sum(kv => kv.Value);
            double allTopicsWeightedAverage = allTopicsMessageCount > 0
                ? allTopicsTotalSum / allTopicsMessageCount
                : 0;

            foreach (string topic in orderedTopics)
            {
                List<double> minuteTotals = topicMinuteTotals[topic];
                double sum = minuteTotals.Sum();
                double average = minuteTotals.Count > 0 ? sum / minuteTotals.Count : 0;
                if (string.Equals(topic, AllTopicsName, StringComparison.Ordinal))
                    average = allTopicsWeightedAverage;
                double maxSample = parseResult.TopicMaxSampleTotals.TryGetValue(topic, out double maxVal)
                    ? maxVal
                    : 0;

                result.Add(new ThreadProcessedEntry
                {
                    Key = topic,
                    Name = topic,
                    CpuPercentSum = sum,
                    CpuPercentAver = average,
                    CpuPercentMax = maxSample,
                    Count = minuteTotals.Count,
                    IsMain = string.Equals(topic, AllTopicsName, StringComparison.Ordinal),
                    Selected = string.Equals(topic, AllTopicsName, StringComparison.Ordinal)
                });
            }

            for (int index = 0; index < result.Count; index++)
                result[index].OriginalOrder = index;

            var textSummary = new List<TopicTextSummaryEntry>();
            foreach (string topic in orderedTopics)
            {
                int count = topicCountTotals.TryGetValue(topic, out int c) ? c : 0;
                double firstTotal = topicFirstTotals.TryGetValue(topic, out double f) ? f : 0;
                double secondTotal = topicSecondTotals.TryGetValue(topic, out double s) ? s : 0;
                double thirdTotal = topicThirdTotals.TryGetValue(topic, out double t) ? t : 0;

                if (string.Equals(topic, AllTopicsName, StringComparison.Ordinal))
                {
                    count = topicCountTotals
                        .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                        .Sum(kv => kv.Value);
                    firstTotal = topicFirstTotals
                        .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                        .Sum(kv => kv.Value);
                    secondTotal = topicSecondTotals
                        .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                        .Sum(kv => kv.Value);
                    thirdTotal = topicThirdTotals
                        .Where(kv => !string.Equals(kv.Key, AllTopicsName, StringComparison.Ordinal))
                        .Sum(kv => kv.Value);
                }

                double divisor = count > 0 ? count : 1;
                double deliverAver = firstTotal / divisor;
                double uncompressAver = secondTotal / divisor;
                double renderAver = thirdTotal / divisor;
                double totalAver = deliverAver + uncompressAver + renderAver;
                textSummary.Add(new TopicTextSummaryEntry
                {
                    Topic = topic,
                    Count = count,
                    Deliver = deliverAver,
                    Uncompress = uncompressAver,
                    Render = renderAver,
                    TotalAver = totalAver,
                    AbsoluteMax = parseResult.TopicMaxSampleTotals.TryGetValue(topic, out double maxVal) ? maxVal : 0
                });
            }

            e.Result = new ProcessingResult
            {
                Threads = result,
                TextSummaryRows = textSummary
            };
            worker.ReportProgress(100, $"{totalMinutes:N0} / {totalMinutes:N0} minutes");
        }

        private void ProcessorWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressAnalysis.Value = e.ProgressPercentage;
            if (e.UserState is string sampleProgress)
                SetLineCountLabel(sampleProgress);
        }

        private void ProcessorWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            SetParserUiRunning(false);

            if (e.Cancelled)
            {
                ClearSampleCpuChart();
                lblParserStatus.Text = "Cancelled.";
                ClearLineCountLabel();
                progressAnalysis.Value = 0;
                return;
            }

            if (e.Error != null)
            {
                ClearSampleCpuChart();
                lblParserStatus.Text = $"Process error: {e.Error.Message}";
                ClearLineCountLabel();
                progressAnalysis.Value = 0;
                return;
            }

            lblParserStatus.Text = "Ready.";
            ClearLineCountLabel();
            progressAnalysis.Value = 100;

            if (e.Result is not ProcessingResult processingResult)
            {
                dgvThreadsSummary.DataSource = null;
                dgvTextSummary.DataSource = null;
                BuildSampleCpuStackChart(minuteTopicAggregates);
                return;
            }

            processedThreads = processingResult.Threads;
            textSummaryRows = processingResult.TextSummaryRows;
            threadSortState = ThreadSortState.None;
            BindThreadsSummaryGrid();
            BindTextSummaryGrid();
            btnRebuildChart.Enabled = processedThreads.Count > 0;
            BuildSampleCpuStackChart(minuteTopicAggregates);
        }

        private void ClearSampleCpuChart()
        {
            chartCpuStacked.ChartAreas.Clear();
            chartCpuStacked.Series.Clear();
            chartCpuStacked.Legends.Clear();
        }

        private const int SampleChartBarPx = 10;
        private const int SampleChartGapPx = 3;
        private const int SampleChartMinuteGroupGapUnits = 1;
        private const int SampleChartMaxBars = 200;
        private const int SampleChartYGutterPx = 78;
        private const int SampleChartRightPadPx = 20;

        private void UpdateSampleChartScaleView()
        {
            if (chartCpuStacked.ChartAreas.Count == 0 || chartCpuStacked.Series.Count == 0)
                return;

            var area = chartCpuStacked.ChartAreas[0];
            var series = chartCpuStacked.Series[0];
            int n = series.Points.Count;
            if (!double.IsNaN(area.AxisX.Maximum) && !double.IsNaN(area.AxisX.Minimum) && area.AxisX.Maximum > area.AxisX.Minimum)
                n = Math.Max(n, (int)Math.Ceiling(area.AxisX.Maximum - area.AxisX.Minimum + 1));
            if (n <= 0)
                return;

            int viewportPlotPx = Math.Max(1, chartCpuStacked.ClientSize.Width - SampleChartYGutterPx - SampleChartRightPadPx);
            int visibleCategories = Math.Max(1, (viewportPlotPx + SampleChartGapPx) / (SampleChartBarPx + SampleChartGapPx));
            int scaleViewSize = Math.Min(n, visibleCategories);
            double currentPosition = area.AxisX.ScaleView.Position;
            if (double.IsNaN(currentPosition) || double.IsInfinity(currentPosition))
                currentPosition = 0;
            double maxPosition = Math.Max(0, n - scaleViewSize);

            area.AxisX.ScaleView.Size = scaleViewSize;
            area.AxisX.ScaleView.Position = Math.Min(Math.Max(0, currentPosition), maxPosition);
        }

        private sealed class ChartPointMeta
        {
            public DateTime Minute { get; init; }
            public string Topic { get; init; } = string.Empty;
            public string SegmentName { get; init; } = string.Empty;
            public double SegmentValue { get; init; }
            public double TotalValue { get; init; }
        }

        private sealed class AxisLabelRange
        {
            public int StartX { get; init; }
            public int EndX { get; init; }
            public string Label { get; init; } = string.Empty;
        }

        private static Color WithAlpha(Color baseColor, int alpha)
            => Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        private void BuildSampleCpuStackChart(SortedDictionary<DateTime, Dictionary<string, TopicMinuteAggregate>> data)
        {
            ClearSampleCpuChart();

            var area = new ChartArea("MainArea") { BackColor = Color.White };
            area.AxisX.Title = "Minute";
            area.AxisX.IsMarginVisible = false;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.MinorTickMark.Enabled = false;
            area.AxisX.IsLabelAutoFit = true;
            area.AxisX.LabelAutoFitStyle = LabelAutoFitStyles.LabelsAngleStep30 | LabelAutoFitStyles.DecreaseFont;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            area.AxisX.LabelStyle.Angle = -45;
            area.AxisY.Title = "ms";
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = double.NaN;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartCpuStacked.ChartAreas.Add(area);

            var selectedTopics = processedThreads.Where(t => t.Selected).ToList();
            EnsureSelectedThreadColorsAssigned();
            int totalMinutes = data.Count;
            if (totalMinutes == 0 || selectedTopics.Count == 0)
            {
                UpdateDisplayedSamplesLabel(0, totalMinutes);
                return;
            }

            int msFilterThreshold = GetCpuFilterThreshold();
            var stage1 = CreateStackSeries("Segment 1", ChartHatchStyle.ForwardDiagonal);
            var stage2 = CreateStackSeries("Segment 2", ChartHatchStyle.DiagonalCross);
            var stage3 = CreateStackSeries("Segment 3", ChartHatchStyle.BackwardDiagonal);

            int xIndex = 0;
            bool hasAnyBar = false;
            int displayedBars = 0;
            int totalCandidateBars = 0;
            var labelRanges = new List<AxisLabelRange>();
            foreach ((DateTime minute, Dictionary<string, TopicMinuteAggregate> byTopic) in data)
            {
                bool minuteHasBar = false;
                int minuteStart = xIndex;
                foreach (ThreadProcessedEntry topic in selectedTopics)
                {
                    if (!byTopic.TryGetValue(topic.Key, out TopicMinuteAggregate? aggregate))
                        continue;

                    double divisor = aggregate.SampleCount > 0 ? aggregate.SampleCount : 1;
                    double firstMs = aggregate.FirstMs / divisor;
                    double secondMs = aggregate.SecondMs / divisor;
                    double thirdMs = aggregate.ThirdMs / divisor;
                    double totalMs = firstMs + secondMs + thirdMs;

                    totalCandidateBars++;
                    if (totalMs < msFilterThreshold)
                        continue;

                    Color baseColor = topic.IsMain
                        ? Color.LightGray
                        : selectedThreadColors.TryGetValue(topic.Key, out Color color) ? color : Color.Gray;

                    AddSegmentPoint(stage1, xIndex, string.Empty, baseColor, firstMs, totalMs, minute, topic.Key, "First");
                    AddSegmentPoint(stage2, xIndex, string.Empty, baseColor, secondMs, totalMs, minute, topic.Key, "Second");
                    AddSegmentPoint(stage3, xIndex, string.Empty, baseColor, thirdMs, totalMs, minute, topic.Key, "Third");
                    xIndex++;
                    displayedBars++;
                    hasAnyBar = true;
                    minuteHasBar = true;
                }

                if (minuteHasBar)
                {
                    int minuteEnd = xIndex - 1;
                    labelRanges.Add(new AxisLabelRange
                    {
                        StartX = minuteStart,
                        EndX = minuteEnd,
                        Label = minute.ToString("HH:mm")
                    });
                    xIndex += SampleChartMinuteGroupGapUnits;
                }
            }

            chartCpuStacked.Series.Add(stage1);
            chartCpuStacked.Series.Add(stage2);
            chartCpuStacked.Series.Add(stage3);
            if (hasAnyBar)
                xIndex -= SampleChartMinuteGroupGapUnits; // remove trailing gap
            UpdateDisplayedSamplesLabel(displayedBars, totalCandidateBars);

            if (xIndex > 0)
            {
                area.AxisX.Minimum = -0.5;
                area.AxisX.Maximum = xIndex - 0.5;
                area.AxisX.CustomLabels.Clear();
                foreach (AxisLabelRange labelRange in labelRanges)
                {
                    area.AxisX.CustomLabels.Add(new CustomLabel(
                        labelRange.StartX - 0.5,
                        labelRange.EndX + 0.5,
                        labelRange.Label,
                        0,
                        LabelMarkStyle.None));
                }
                area.AxisX.ScrollBar.Enabled = true;
                area.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
                area.AxisX.ScrollBar.IsPositionedInside = false;
                area.AxisX.ScaleView.Zoomable = true;
                area.CursorX.IsUserSelectionEnabled = false;

                UpdateSampleChartScaleView();
                area.AxisX.ScaleView.Position = 0;
            }
        }

        private static Series CreateStackSeries(string name, ChartHatchStyle hatchStyle)
        {
            var series = new Series(name)
            {
                ChartType = SeriesChartType.StackedColumn,
                BorderColor = Color.Black,
                BorderWidth = 1,
                IsValueShownAsLabel = false,
                IsXValueIndexed = false,
                BackHatchStyle = hatchStyle
            };
            series["PixelPointWidth"] = SampleChartBarPx.ToString();
            return series;
        }

        private static void AddSegmentPoint(
            Series series,
            int xIndex,
            string axisLabel,
            Color baseColor,
            double value,
            double total,
            DateTime minute,
            string topicKey,
            string segmentName)
        {
            var point = new DataPoint(xIndex, value)
            {
                AxisLabel = axisLabel,
                Color = WithAlpha(baseColor, 210),
                Tag = new ChartPointMeta
                {
                    Minute = minute,
                    Topic = topicKey,
                    SegmentName = segmentName,
                    SegmentValue = value,
                    TotalValue = total
                }
            };
            series.Points.Add(point);
        }

        private void RefreshThreadColorColumn()
        {
            EnsureSelectedThreadColorsAssigned();

            foreach (DataGridViewRow row in dgvThreadsSummary.Rows)
            {
                if (row.DataBoundItem is not ThreadProcessedEntry thread)
                    continue;

                var colorCell = row.Cells["colColor"];
                if (thread.Selected && (thread.IsMain || selectedThreadColors.TryGetValue(thread.Key, out _)))
                {
                    Color color = thread.IsMain
                        ? Color.LightGray
                        : selectedThreadColors[thread.Key];
                    colorCell.Value = "■";
                    colorCell.Style.ForeColor = color;
                    colorCell.Style.SelectionForeColor = color;
                    colorCell.Style.BackColor = Color.White;
                    colorCell.Style.SelectionBackColor = Color.White;
                    colorCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else
                {
                    colorCell.Value = string.Empty;
                    colorCell.Style.ForeColor = dgvThreadsSummary.DefaultCellStyle.ForeColor;
                    colorCell.Style.SelectionForeColor = dgvThreadsSummary.DefaultCellStyle.SelectionForeColor;
                    colorCell.Style.BackColor = dgvThreadsSummary.DefaultCellStyle.BackColor;
                    colorCell.Style.SelectionBackColor = dgvThreadsSummary.DefaultCellStyle.SelectionBackColor;
                }
            }
        }

        private void ChartCpuStacked_GetToolTipText(object? sender, ToolTipEventArgs e)
        {
            if (e.HitTestResult.ChartElementType != ChartElementType.DataPoint)
                return;

            var hitSeries = e.HitTestResult.Series;
            if (hitSeries is null)
                return;

            int pointIndex = e.HitTestResult.PointIndex;
            if (pointIndex < 0 || pointIndex >= hitSeries.Points.Count)
                return;

            var point = hitSeries.Points[pointIndex];
            if (point.Tag is not ChartPointMeta meta)
                return;
            e.Text =
                $"Minute {meta.Minute:yyyy-MM-dd HH:mm}\n" +
                $"Topic {meta.Topic}\n" +
                $"{meta.SegmentName}: {meta.SegmentValue:F2} ms\n" +
                $"Total: {meta.TotalValue:F2} ms";
        }

        private const int LineCountGapPx = 8;

        private void LayoutLineCountAfterStatus()
        {
            lblLineCount.Left = lblParserStatus.Right + LineCountGapPx;
            lblLineCount.Top = lblParserStatus.Top;
        }

        private void SetLineCountLabel(string lineProgress)
        {
            lblLineCount.Text = lineProgress;
            lblLineCount.Visible = true;
            LayoutLineCountAfterStatus();
        }

        private void ClearLineCountLabel()
        {
            lblLineCount.Text = string.Empty;
            lblLineCount.Visible = false;
        }

        private void SetParserUiRunning(bool running)
        {
            btnSelectFile.Enabled = !running;
            btnCancelAnalysis.Enabled = running;
            Cursor = running ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
