using System.Windows.Forms.DataVisualization.Charting;
using _13StatParser.Models;

namespace _13StatParser
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Button btnSelectFile;
        private TextBox txtFilePath;
        private Label lblFilePath;
        private ProgressBar progressAnalysis;
        private Label lblParserStatus;
        private Label lblLineCount;
        private Button btnCancelAnalysis;
        private TabControl tabControlMain;
        private TabPage tabPageThreadsChart;
        private TabPage tabPageTextSummary;
        private Panel panelThreadControls;
        private Button btnDeselectAll;
        private Button btnResetSort;
        private Label lblFilterCpu;
        private ComboBox cmbFilterCpuThreshold;
        private Label lblDisplayedSamples;
        private Button btnRebuildChart;
        private SplitContainer splitContainerThreadsChart;
        private DataGridView dgvThreadsSummary;
        private DataGridView dgvTextSummary;
        private Chart chartCpuStacked;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            btnSelectFile = new Button();
            txtFilePath = new TextBox();
            lblFilePath = new Label();
            progressAnalysis = new ProgressBar();
            lblParserStatus = new Label();
            lblLineCount = new Label();
            btnCancelAnalysis = new Button();
            tabControlMain = new TabControl();
            tabPageThreadsChart = new TabPage();
            tabPageTextSummary = new TabPage();
            panelThreadControls = new Panel();
            btnDeselectAll = new Button();
            btnResetSort = new Button();
            lblFilterCpu = new Label();
            cmbFilterCpuThreshold = new ComboBox();
            lblDisplayedSamples = new Label();
            btnRebuildChart = new Button();
            splitContainerThreadsChart = new SplitContainer();
            dgvThreadsSummary = new DataGridView();
            dgvTextSummary = new DataGridView();
            chartCpuStacked = new Chart();
            tabPageThreadsChart.SuspendLayout();
            panelThreadControls.SuspendLayout();
            splitContainerThreadsChart.Panel1.SuspendLayout();
            splitContainerThreadsChart.Panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerThreadsChart).BeginInit();
            splitContainerThreadsChart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvThreadsSummary).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTextSummary).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chartCpuStacked).BeginInit();
            tabControlMain.SuspendLayout();
            SuspendLayout();
            //
            // btnSelectFile
            //
            btnSelectFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectFile.Location = new Point(1088, 11);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(100, 25);
            btnSelectFile.TabIndex = 2;
            btnSelectFile.Text = "Select File";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            //
            // txtFilePath
            //
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilePath.Location = new Point(73, 12);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(1009, 23);
            txtFilePath.TabIndex = 1;
            //
            // lblFilePath
            //
            lblFilePath.AutoSize = true;
            lblFilePath.Location = new Point(12, 15);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Size = new Size(55, 15);
            lblFilePath.TabIndex = 0;
            lblFilePath.Text = "File Path:";
            //
            // progressAnalysis
            //
            progressAnalysis.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressAnalysis.Location = new Point(12, 45);
            progressAnalysis.Name = "progressAnalysis";
            progressAnalysis.Size = new Size(1070, 23);
            progressAnalysis.Style = ProgressBarStyle.Blocks;
            progressAnalysis.TabIndex = 3;
            //
            // lblParserStatus
            //
            lblParserStatus.AutoSize = true;
            lblParserStatus.Location = new Point(12, 74);
            lblParserStatus.MaximumSize = new Size(800, 0);
            lblParserStatus.Name = "lblParserStatus";
            lblParserStatus.Size = new Size(0, 15);
            lblParserStatus.TabIndex = 4;
            //
            // lblLineCount
            //
            lblLineCount.AutoSize = true;
            lblLineCount.Location = new Point(12, 74);
            lblLineCount.Name = "lblLineCount";
            lblLineCount.Size = new Size(0, 15);
            lblLineCount.TabIndex = 5;
            lblLineCount.Visible = false;
            //
            // btnCancelAnalysis
            //
            btnCancelAnalysis.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancelAnalysis.Enabled = false;
            btnCancelAnalysis.Location = new Point(1088, 44);
            btnCancelAnalysis.Name = "btnCancelAnalysis";
            btnCancelAnalysis.Size = new Size(100, 25);
            btnCancelAnalysis.TabIndex = 6;
            btnCancelAnalysis.Text = "Cancel";
            btnCancelAnalysis.UseVisualStyleBackColor = true;
            btnCancelAnalysis.Click += btnCancelAnalysis_Click;
            //
            // tabControlMain
            //
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlMain.Controls.Add(tabPageThreadsChart);
            tabControlMain.Controls.Add(tabPageTextSummary);
            tabControlMain.Location = new Point(12, 96);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1176, 492);
            tabControlMain.TabIndex = 7;
            //
            // tabPageThreadsChart
            //
            tabPageThreadsChart.Controls.Add(splitContainerThreadsChart);
            tabPageThreadsChart.Controls.Add(panelThreadControls);
            tabPageThreadsChart.Location = new Point(4, 24);
            tabPageThreadsChart.Name = "tabPageThreadsChart";
            tabPageThreadsChart.Padding = new Padding(3);
            tabPageThreadsChart.Size = new Size(1168, 464);
            tabPageThreadsChart.TabIndex = 0;
            tabPageThreadsChart.Text = "Threads & chart";
            tabPageThreadsChart.UseVisualStyleBackColor = true;
            //
            // tabPageTextSummary
            //
            tabPageTextSummary.Controls.Add(dgvTextSummary);
            tabPageTextSummary.Location = new Point(4, 24);
            tabPageTextSummary.Name = "tabPageTextSummary";
            tabPageTextSummary.Padding = new Padding(3);
            tabPageTextSummary.Size = new Size(1168, 464);
            tabPageTextSummary.TabIndex = 1;
            tabPageTextSummary.Text = "Text Summary";
            tabPageTextSummary.UseVisualStyleBackColor = true;
            //
            // panelThreadControls
            //
            panelThreadControls.Controls.Add(btnDeselectAll);
            panelThreadControls.Controls.Add(lblDisplayedSamples);
            panelThreadControls.Controls.Add(cmbFilterCpuThreshold);
            panelThreadControls.Controls.Add(lblFilterCpu);
            panelThreadControls.Controls.Add(btnRebuildChart);
            panelThreadControls.Controls.Add(btnResetSort);
            panelThreadControls.Dock = DockStyle.Top;
            panelThreadControls.Location = new Point(3, 3);
            panelThreadControls.Name = "panelThreadControls";
            panelThreadControls.Size = new Size(1162, 33);
            panelThreadControls.TabIndex = 1;
            //
            // btnDeselectAll
            //
            btnDeselectAll.Location = new Point(6, 4);
            btnDeselectAll.Name = "btnDeselectAll";
            btnDeselectAll.Size = new Size(86, 25);
            btnDeselectAll.TabIndex = 0;
            btnDeselectAll.Text = "Deselect all";
            btnDeselectAll.UseVisualStyleBackColor = true;
            btnDeselectAll.Click += btnDeselectAll_Click;
            //
            // btnResetSort
            //
            btnResetSort.Enabled = false;
            btnResetSort.Location = new Point(98, 4);
            btnResetSort.Name = "btnResetSort";
            btnResetSort.Size = new Size(82, 25);
            btnResetSort.TabIndex = 1;
            btnResetSort.Text = "Reset sort";
            btnResetSort.UseVisualStyleBackColor = true;
            btnResetSort.Click += btnResetSort_Click;
            //
            // lblFilterCpu
            //
            lblFilterCpu.AutoSize = true;
            lblFilterCpu.Location = new Point(186, 9);
            lblFilterCpu.Name = "lblFilterCpu";
            lblFilterCpu.Size = new Size(81, 15);
            lblFilterCpu.TabIndex = 2;
            lblFilterCpu.Text = "Filter by ms";
            //
            // cmbFilterCpuThreshold
            //
            cmbFilterCpuThreshold.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterCpuThreshold.FormattingEnabled = true;
            cmbFilterCpuThreshold.Location = new Point(273, 5);
            cmbFilterCpuThreshold.Name = "cmbFilterCpuThreshold";
            cmbFilterCpuThreshold.Size = new Size(70, 23);
            cmbFilterCpuThreshold.TabIndex = 3;
            //
            // lblDisplayedSamples
            //
            lblDisplayedSamples.AutoSize = true;
            lblDisplayedSamples.Location = new Point(351, 9);
            lblDisplayedSamples.Name = "lblDisplayedSamples";
            lblDisplayedSamples.Size = new Size(63, 15);
            lblDisplayedSamples.TabIndex = 4;
            lblDisplayedSamples.Text = "0 out 0";
            //
            // btnRebuildChart
            //
            btnRebuildChart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRebuildChart.Enabled = false;
            btnRebuildChart.Location = new Point(1068, 4);
            btnRebuildChart.Name = "btnRebuildChart";
            btnRebuildChart.Size = new Size(88, 25);
            btnRebuildChart.TabIndex = 1;
            btnRebuildChart.Text = "Rebuild chart";
            btnRebuildChart.UseVisualStyleBackColor = true;
            btnRebuildChart.Click += btnRebuildChart_Click;
            //
            // splitContainerThreadsChart
            //
            splitContainerThreadsChart.Dock = DockStyle.Fill;
            splitContainerThreadsChart.FixedPanel = FixedPanel.Panel1;
            splitContainerThreadsChart.IsSplitterFixed = true;
            splitContainerThreadsChart.Location = new Point(3, 36);
            splitContainerThreadsChart.Name = "splitContainerThreadsChart";
            splitContainerThreadsChart.Panel1.Controls.Add(dgvThreadsSummary);
            splitContainerThreadsChart.Panel1MinSize = 200;
            splitContainerThreadsChart.Panel2.AutoScroll = true;
            splitContainerThreadsChart.Panel2.Controls.Add(chartCpuStacked);
            splitContainerThreadsChart.Panel2MinSize = 120;
            splitContainerThreadsChart.Size = new Size(1162, 425);
            splitContainerThreadsChart.SplitterDistance = 280;
            splitContainerThreadsChart.SplitterWidth = 5;
            splitContainerThreadsChart.TabIndex = 0;
            splitContainerThreadsChart.TabStop = false;
            //
            // dgvThreadsSummary
            //
            dgvThreadsSummary.AllowUserToAddRows = false;
            dgvThreadsSummary.AllowUserToDeleteRows = false;
            dgvThreadsSummary.AllowUserToResizeRows = false;
            dgvThreadsSummary.AutoGenerateColumns = false;
            dgvThreadsSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvThreadsSummary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            colSelected.DataPropertyName = nameof(ThreadProcessedEntry.Selected);
            colSelected.FillWeight = 12F;
            colSelected.HeaderText = "Sel";
            colSelected.Name = "colSelected";
            colSelected.Resizable = DataGridViewTriState.True;
            colSelected.SortMode = DataGridViewColumnSortMode.Automatic;
            colColor.FillWeight = 12F;
            colColor.HeaderText = "";
            colColor.Name = "colColor";
            colColor.ReadOnly = true;
            colColor.SortMode = DataGridViewColumnSortMode.NotSortable;
            colName.DataPropertyName = nameof(ThreadProcessedEntry.Name);
            colName.FillWeight = 50F;
            colName.HeaderText = "Name";
            colName.Name = "colName";
            colName.ReadOnly = true;
            colCpuAvg.DataPropertyName = nameof(ThreadProcessedEntry.CpuPercentAver);
            colCpuAvg.DefaultCellStyle.Format = "F2";
            colCpuAvg.FillWeight = 25F;
            colCpuAvg.HeaderText = "Avg ms";
            colCpuAvg.Name = "colCpuAvg";
            colCpuAvg.ReadOnly = true;
            colCpuAvg.SortMode = DataGridViewColumnSortMode.Programmatic;
            colCpuMax.DataPropertyName = nameof(ThreadProcessedEntry.CpuPercentMax);
            colCpuMax.DefaultCellStyle.Format = "F2";
            colCpuMax.FillWeight = 25F;
            colCpuMax.HeaderText = "Max ms";
            colCpuMax.Name = "colCpuMax";
            colCpuMax.ReadOnly = true;
            colCpuMax.SortMode = DataGridViewColumnSortMode.Programmatic;
            dgvThreadsSummary.Columns.AddRange(new DataGridViewColumn[] {
                colSelected,
                colColor,
                colName,
                colCpuAvg,
                colCpuMax});
            dgvThreadsSummary.Dock = DockStyle.Fill;
            dgvThreadsSummary.Name = "dgvThreadsSummary";
            dgvThreadsSummary.ReadOnly = false;
            dgvThreadsSummary.RowHeadersVisible = false;
            dgvThreadsSummary.RowTemplate.Height = 25;
            dgvThreadsSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvThreadsSummary.TabIndex = 0;
            //
            // chartCpuStacked
            //
            chartCpuStacked.Dock = DockStyle.Left;
            chartCpuStacked.Location = new Point(0, 0);
            chartCpuStacked.Name = "chartCpuStacked";
            chartCpuStacked.Size = new Size(845, 458);
            chartCpuStacked.TabIndex = 0;
            chartCpuStacked.TabStop = false;
            //
            // dgvTextSummary
            //
            dgvTextSummary.AllowUserToAddRows = false;
            dgvTextSummary.AllowUserToDeleteRows = false;
            dgvTextSummary.AllowUserToResizeRows = false;
            dgvTextSummary.AutoGenerateColumns = false;
            dgvTextSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTextSummary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            txtColTopic.DataPropertyName = nameof(TopicTextSummaryEntry.Topic);
            txtColTopic.HeaderText = "Topic";
            txtColTopic.Name = "txtColTopic";
            txtColTopic.ReadOnly = true;
            txtColCount.DataPropertyName = nameof(TopicTextSummaryEntry.Count);
            txtColCount.HeaderText = "Count";
            txtColCount.Name = "txtColCount";
            txtColCount.ReadOnly = true;
            txtColDeliver.DataPropertyName = nameof(TopicTextSummaryEntry.Deliver);
            txtColDeliver.DefaultCellStyle.Format = "F2";
            txtColDeliver.HeaderText = "Deliver";
            txtColDeliver.Name = "txtColDeliver";
            txtColDeliver.ReadOnly = true;
            txtColUncompress.DataPropertyName = nameof(TopicTextSummaryEntry.Uncompress);
            txtColUncompress.DefaultCellStyle.Format = "F2";
            txtColUncompress.HeaderText = "Uncompress";
            txtColUncompress.Name = "txtColUncompress";
            txtColUncompress.ReadOnly = true;
            txtColRender.DataPropertyName = nameof(TopicTextSummaryEntry.Render);
            txtColRender.DefaultCellStyle.Format = "F2";
            txtColRender.HeaderText = "Render";
            txtColRender.Name = "txtColRender";
            txtColRender.ReadOnly = true;
            txtColTotalAver.DataPropertyName = nameof(TopicTextSummaryEntry.TotalAver);
            txtColTotalAver.DefaultCellStyle.Format = "F2";
            txtColTotalAver.HeaderText = "TotalAvr";
            txtColTotalAver.Name = "txtColTotalAver";
            txtColTotalAver.ReadOnly = true;
            txtColAbsoluteMax.DataPropertyName = nameof(TopicTextSummaryEntry.AbsoluteMax);
            txtColAbsoluteMax.DefaultCellStyle.Format = "F2";
            txtColAbsoluteMax.HeaderText = "Absolute Max";
            txtColAbsoluteMax.Name = "txtColAbsoluteMax";
            txtColAbsoluteMax.ReadOnly = true;
            dgvTextSummary.Columns.AddRange(new DataGridViewColumn[] {
                txtColTopic,
                txtColCount,
                txtColDeliver,
                txtColUncompress,
                txtColRender,
                txtColTotalAver,
                txtColAbsoluteMax});
            dgvTextSummary.Dock = DockStyle.Fill;
            dgvTextSummary.Location = new Point(3, 3);
            dgvTextSummary.Name = "dgvTextSummary";
            dgvTextSummary.ReadOnly = true;
            dgvTextSummary.RowHeadersVisible = false;
            dgvTextSummary.RowTemplate.Height = 25;
            dgvTextSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTextSummary.Size = new Size(1162, 458);
            dgvTextSummary.TabIndex = 0;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 600);
            Controls.Add(lblFilePath);
            Controls.Add(txtFilePath);
            Controls.Add(btnSelectFile);
            Controls.Add(progressAnalysis);
            Controls.Add(lblParserStatus);
            Controls.Add(lblLineCount);
            Controls.Add(btnCancelAnalysis);
            Controls.Add(tabControlMain);
            Name = "Form1";
            Text = "SDP Client Logs Parser";
            panelThreadControls.ResumeLayout(false);
            tabPageThreadsChart.ResumeLayout(false);
            splitContainerThreadsChart.Panel1.ResumeLayout(false);
            splitContainerThreadsChart.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerThreadsChart).EndInit();
            splitContainerThreadsChart.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvThreadsSummary).EndInit();
            tabPageTextSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTextSummary).EndInit();
            ((System.ComponentModel.ISupportInitialize)chartCpuStacked).EndInit();
            tabControlMain.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridViewCheckBoxColumn colSelected = new();
        private DataGridViewTextBoxColumn colColor = new();
        private DataGridViewTextBoxColumn colName = new();
        private DataGridViewTextBoxColumn colCpuAvg = new();
        private DataGridViewTextBoxColumn colCpuMax = new();
        private DataGridViewTextBoxColumn txtColTopic = new();
        private DataGridViewTextBoxColumn txtColCount = new();
        private DataGridViewTextBoxColumn txtColDeliver = new();
        private DataGridViewTextBoxColumn txtColUncompress = new();
        private DataGridViewTextBoxColumn txtColRender = new();
        private DataGridViewTextBoxColumn txtColTotalAver = new();
        private DataGridViewTextBoxColumn txtColAbsoluteMax = new();

        #endregion
    }
}
