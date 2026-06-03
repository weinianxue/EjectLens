using EjectLens.Utils;

namespace EjectLens;

public sealed partial class MainForm
{
    private void InitializeLayout()
    {
        // Main layout: rows = top (auto), center (fill), bottom (auto).
        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.ColumnCount = 1;
        _mainLayout.RowCount = 3;
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mainLayout.Padding = new Padding(8);

        // Top panel: drive selector, refresh, time range, scan button.
        _topPanel.Dock = DockStyle.Fill;
        _topPanel.AutoSize = true;
        _topPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _topPanel.ColumnCount = 8;
        _topPanel.RowCount = 1;
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        var driveLabel = new Label
        {
            Text = "Drive:",
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Padding = new Padding(0, 4, 4, 0)
        };
        _topPanel.Controls.Add(driveLabel, 0, 0);

        _driveCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _driveCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _driveCombo.SelectedIndexChanged += DriveCombo_SelectedIndexChanged;
        _topPanel.Controls.Add(_driveCombo, 1, 0);

        _refreshButton.Text = "Refresh";
        _refreshButton.AutoSize = true;
        _refreshButton.Click += RefreshButton_Click;
        _topPanel.Controls.Add(_refreshButton, 2, 0);

        var timeLabel = new Label
        {
            Text = "Time range:",
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Padding = new Padding(12, 4, 4, 0)
        };
        _topPanel.Controls.Add(timeLabel, 4, 0);

        _timeRangeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (TimeRange range in Enum.GetValues<TimeRange>())
        {
            _timeRangeCombo.Items.Add(new TimeRangeItem(range));
        }
        _timeRangeCombo.SelectedIndex = (int)TimeRange.Hours2;
        _topPanel.Controls.Add(_timeRangeCombo, 5, 0);

        _scanEventsButton.Text = "Scan Events";
        _scanEventsButton.AutoSize = true;
        _scanEventsButton.Click += ScanEventsButton_Click;
        _topPanel.Controls.Add(_scanEventsButton, 6, 0);

        _centerSplit.Dock = DockStyle.Fill;
        _centerSplit.Orientation = Orientation.Vertical;
        _centerSplit.SplitterDistance = 700;

        InitializeEventsGrid();
        _centerSplit.Panel1.Controls.Add(_eventsGrid);

        _rightPanel.Dock = DockStyle.Fill;
        _rightPanel.ColumnCount = 1;
        _rightPanel.RowCount = 2;
        _rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _detailLabel.Text = "Event Details";
        _detailLabel.AutoSize = true;
        _detailLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _rightPanel.Controls.Add(_detailLabel, 0, 0);

        _detailTextBox.Multiline = true;
        _detailTextBox.ReadOnly = true;
        _detailTextBox.ScrollBars = ScrollBars.Vertical;
        _detailTextBox.Dock = DockStyle.Fill;
        _detailTextBox.Font = new Font("Consolas", 9F);
        _detailTextBox.WordWrap = true;
        _rightPanel.Controls.Add(_detailTextBox, 0, 1);

        _centerSplit.Panel2.Controls.Add(_rightPanel);

        _bottomPanel.Dock = DockStyle.Fill;
        _bottomPanel.AutoSize = true;
        _bottomPanel.FlowDirection = FlowDirection.LeftToRight;
        _bottomPanel.Padding = new Padding(0, 6, 0, 0);

        _copyReportButton.Text = "Copy Report";
        _copyReportButton.AutoSize = true;
        _copyReportButton.Click += CopyReportButton_Click;

        _saveReportButton.Text = "Save Report";
        _saveReportButton.AutoSize = true;
        _saveReportButton.Click += SaveReportButton_Click;

        _openTaskManagerButton.Text = "Open Task Manager";
        _openTaskManagerButton.AutoSize = true;
        _openTaskManagerButton.Click += OpenTaskManagerButton_Click;

        _scanFilesButton.Text = "Scan Selected Path...";
        _scanFilesButton.AutoSize = true;
        _scanFilesButton.Click += ScanFilesButton_Click;

        _statusLabel.AutoSize = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(12, 4, 0, 0);

        _bottomPanel.Controls.Add(_copyReportButton);
        _bottomPanel.Controls.Add(_saveReportButton);
        _bottomPanel.Controls.Add(_openTaskManagerButton);
        _bottomPanel.Controls.Add(_scanFilesButton);
        _bottomPanel.Controls.Add(_statusLabel);

        _mainLayout.Controls.Add(_topPanel, 0, 0);
        _mainLayout.Controls.Add(_centerSplit, 0, 1);
        _mainLayout.Controls.Add(_bottomPanel, 0, 2);

        Controls.Add(_mainLayout);
    }

    private void InitializeEventsGrid()
    {
        _eventsGrid.Dock = DockStyle.Fill;
        _eventsGrid.ReadOnly = true;
        _eventsGrid.AllowUserToAddRows = false;
        _eventsGrid.AllowUserToDeleteRows = false;
        _eventsGrid.AllowUserToResizeRows = false;
        _eventsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _eventsGrid.RowHeadersVisible = false;
        _eventsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _eventsGrid.MultiSelect = false;

        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status", HeaderText = "Status", DataPropertyName = "MatchStatus",
            FillWeight = 80, MinimumWidth = 80
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Time", HeaderText = "Time", DataPropertyName = "EventTimeFormatted",
            FillWeight = 110, MinimumWidth = 100
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Process", HeaderText = "Process", DataPropertyName = "ProcessName",
            FillWeight = 100, MinimumWidth = 80
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PID", HeaderText = "PID", DataPropertyName = "ProcessId",
            FillWeight = 50, MinimumWidth = 50
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Path", HeaderText = "Path", DataPropertyName = "ApplicationPath",
            FillWeight = 140, MinimumWidth = 100
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CommandLine", HeaderText = "Command Line", DataPropertyName = "CommandLine",
            FillWeight = 120, MinimumWidth = 80
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Device", HeaderText = "Affected Device", DataPropertyName = "DeviceInstanceId",
            FillWeight = 120, MinimumWidth = 100
        });
        _eventsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "State", HeaderText = "State", DataPropertyName = "ProcessState",
            FillWeight = 50, MinimumWidth = 50
        });

        _eventsGrid.SelectionChanged += EventsGrid_SelectionChanged;
    }
}
