using System.ComponentModel;
using System.Diagnostics;
using EjectLens.Models;
using EjectLens.Services;
using EjectLens.Utils;

namespace EjectLens;

public sealed class MainForm : Form
{
    // Services.
    private readonly RemovableDriveService _driveService = new();
    private readonly EventLogService _eventLogService = new();
    private readonly RestartManagerService _restartManagerService = new();

    // Data.
    private List<DriveInfoModel> _drives = [];
    private List<EjectBlockEvent> _events = [];
    private DriveInfoModel? _selectedDrive;

    // UI controls.
    private readonly ComboBox _driveCombo = new();
    private readonly Button _refreshButton = new();
    private readonly ComboBox _timeRangeCombo = new();
    private readonly Button _scanEventsButton = new();
    private readonly DataGridView _eventsGrid = new();
    private readonly TextBox _detailTextBox = new();
    private readonly Button _copyReportButton = new();
    private readonly Button _saveReportButton = new();
    private readonly Button _openTaskManagerButton = new();
    private readonly Button _scanFilesButton = new();
    private readonly Label _statusLabel = new();
    private readonly Label _detailLabel = new();

    // Layout.
    private readonly TableLayoutPanel _mainLayout = new();
    private readonly TableLayoutPanel _topPanel = new();
    private readonly SplitContainer _centerSplit = new();
    private readonly TableLayoutPanel _rightPanel = new();
    private readonly FlowLayoutPanel _bottomPanel = new();

    public MainForm()
    {
        InitializeForm();
        InitializeLayout();
        LoadDrives();
    }

    private void InitializeForm()
    {
        Text = "EjectLens — USB Safe Removal Diagnostics";
        Size = new Size(1100, 700);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
    }

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

    private void LoadDrives()
    {
        try
        {
            _drives = _driveService.GetRemovableDrives();

            _driveCombo.BeginUpdate();
            _driveCombo.Items.Clear();

            if (_drives.Count == 0)
            {
                _driveCombo.Items.Add("(no removable drives found)");
                _driveCombo.SelectedIndex = 0;
                _selectedDrive = null;
            }
            else
            {
                foreach (var drive in _drives)
                {
                    _driveCombo.Items.Add(drive);
                }
                _driveCombo.SelectedIndex = 0;
            }
            _driveCombo.EndUpdate();
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading drives: {ex.Message}");
        }
    }

    private void DriveCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_driveCombo.SelectedItem is DriveInfoModel drive)
        {
            _selectedDrive = drive;
            UpdateMatchStatus();
        }
        else
        {
            _selectedDrive = null;
        }
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        LoadDrives();
        SetStatus("Drive list refreshed.");
    }

    private void ScanEventsButton_Click(object? sender, EventArgs e)
    {
        SetStatus("Scanning event log...");
        Application.UseWaitCursor = true;

        try
        {
            var range = GetSelectedTimeRange();
            _events = _eventLogService.GetEjectBlockEvents(range);

            foreach (var evt in _events)
            {
                evt.IsProcessRunning = ProcessInfoService.IsProcessRunning(evt.ProcessId);
            }

            UpdateMatchStatus();
            RefreshEventsGrid();

            if (_events.Count == 0)
            {
                SetStatus("No recent eject-blocking events found. "
                    + "If a device was blocked, Windows may not have logged Event ID 225 for it.");
            }
            else
            {
                SetStatus($"Found {_events.Count} event(s). "
                    + "Select a drive above to match events, or browse all recent events below.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error scanning events: {ex.Message}");
        }
        finally
        {
            Application.UseWaitCursor = false;
        }
    }

    private void EventsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_eventsGrid.SelectedRows.Count == 0)
        {
            _detailTextBox.Text = string.Empty;
            return;
        }

        var row = _eventsGrid.SelectedRows[0];
        if (row.DataBoundItem is not EjectBlockEvent evt)
        {
            var idx = row.Index;
            if (idx >= 0 && idx < _events.Count)
                evt = _events[idx];
            else
            {
                _detailTextBox.Text = string.Empty;
                return;
            }
        }

        ShowEventDetail(evt);
    }

    private void UpdateMatchStatus()
    {
        if (_selectedDrive == null)
        {
            foreach (var evt in _events)
                evt.MatchStatus = MatchStatus.Unmatched;
            return;
        }

        var identifiers = VolumeMappingService.GetVolumeIdentifiers(
            _selectedDrive.DriveLetter + "\\");

        foreach (var evt in _events)
        {
            if (string.IsNullOrEmpty(evt.DeviceInstanceId))
            {
                evt.MatchStatus = MatchStatus.PossiblyRelated;
                continue;
            }

            bool matched = identifiers.Any(id =>
                evt.DeviceInstanceId.Contains(id, StringComparison.OrdinalIgnoreCase));

            if (matched)
            {
                evt.MatchStatus = MatchStatus.Matched;
            }
            else
            {
                bool partial = identifiers.Any(id =>
                {
                    if (id.Length < 5) return false;
                    if (id.StartsWith('{') && id.EndsWith('}'))
                        return evt.DeviceInstanceId.Contains(id, StringComparison.OrdinalIgnoreCase);
                    return false;
                });

                evt.MatchStatus = partial ? MatchStatus.PossiblyRelated : MatchStatus.Unmatched;
            }
        }

        RefreshEventsGrid();
    }

    private void RefreshEventsGrid()
    {
        _eventsGrid.DataSource = null;

        if (_events.Count == 0)
        {
            _eventsGrid.DataSource = null;
            return;
        }

        var displayList = _events.Select(e => new
        {
            e.MatchStatus,
            EventTimeFormatted = e.EventTime.ToString("yyyy-MM-dd HH:mm:ss"),
            e.ProcessName,
            e.ProcessId,
            e.ApplicationPath,
            e.CommandLine,
            e.DeviceInstanceId,
            ProcessState = e.IsProcessRunning ? "running" : "exited"
        }).ToList();

        _eventsGrid.DataSource = displayList;

        foreach (DataGridViewRow row in _eventsGrid.Rows)
        {
            if (row.Index >= _events.Count) continue;

            var evt = _events[row.Index];
            row.DefaultCellStyle.BackColor = evt.MatchStatus switch
            {
                MatchStatus.Matched => Color.FromArgb(230, 255, 230),
                MatchStatus.PossiblyRelated => Color.FromArgb(255, 255, 220),
                MatchStatus.Unmatched => Color.FromArgb(250, 250, 250),
                _ => Color.White
            };

            if (!evt.IsProcessRunning)
            {
                row.DefaultCellStyle.ForeColor = Color.Gray;
            }
        }
    }

    private void ShowEventDetail(EjectBlockEvent evt)
    {
        var lines = new List<string>();

        lines.Add($"Status:       {evt.MatchStatus}");
        lines.Add($"Time:         {evt.EventTime:yyyy-MM-dd HH:mm:ss}");
        lines.Add($"Process:      {evt.ProcessName}");
        lines.Add($"PID:          {evt.ProcessId}");
        lines.Add($"State:        {(evt.IsProcessRunning ? "Running" : "Exited")}");

        if (!string.IsNullOrEmpty(evt.ApplicationPath))
        {
            lines.Add($"Path:         {evt.ApplicationPath}");

            try
            {
                var dir = Path.GetDirectoryName(evt.ApplicationPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    lines.Add($"Location:     {dir} (exists)");
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(evt.CommandLine))
            lines.Add($"CmdLine:      {evt.CommandLine}");

        if (!string.IsNullOrEmpty(evt.DeviceInstanceId))
            lines.Add($"Device:       {evt.DeviceInstanceId}");

        var advisory = ProcessInfoService.GetAdvisoryMessage(
            string.IsNullOrEmpty(evt.ApplicationPath) ? evt.ProcessName : evt.ApplicationPath);

        if (!string.IsNullOrEmpty(advisory))
        {
            lines.Add(string.Empty);
            lines.Add($"Advisory:     {advisory}");
        }

        if (!evt.IsParsed && !string.IsNullOrEmpty(evt.RawMessageSummary))
        {
            lines.Add(string.Empty);
            lines.Add("── Raw Event Message (parsing failed) ──");
            lines.Add(evt.RawMessageSummary);
        }

        _detailTextBox.Text = string.Join(Environment.NewLine, lines);
    }

    private void CopyReportButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var report = GenerateReport();
            Clipboard.SetText(report);
            SetStatus("Report copied to clipboard.");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to copy report: {ex.Message}");
        }
    }

    private void SaveReportButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save Diagnostic Report",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"EjectLens_Report_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var report = GenerateReport();
            File.WriteAllText(dialog.FileName, report);
            SetStatus($"Report saved to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to save report: {ex.Message}");
        }
    }

    private void OpenTaskManagerButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start("taskmgr.exe");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to open Task Manager: {ex.Message}");
        }
    }

    private void ScanFilesButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder on the USB drive to scan for file locks.",
            ShowNewFolderButton = false
        };

        if (_selectedDrive != null)
        {
            dialog.SelectedPath = _selectedDrive.DriveLetter + "\\";
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var path = dialog.SelectedPath;
        SetStatus($"Scanning {path} for file locks...");
        Application.UseWaitCursor = true;

        try
        {
            int filesScanned;
            var results = _restartManagerService.ScanFolderForLocks(path, out filesScanned);

            if (results.Count == 0)
            {
                SetStatus($"Scanned {filesScanned} files in {path}. No locking processes found.");
            }
            else
            {
                var names = string.Join(", ",
                    results.Take(5).Select(r => r.ProcessName));
                var extra = results.Count > 5
                    ? $" and {results.Count - 5} more" : "";
                SetStatus($"Scanned {filesScanned} files. "
                    + $"Found {results.Count} process(es) holding locks: {names}{extra}.");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"File Lock Scan Results");
            sb.AppendLine($"Path: {path}");
            sb.AppendLine($"Files scanned: {filesScanned}");
            sb.AppendLine();

            if (results.Count == 0)
            {
                sb.AppendLine("No locking processes found.");
            }
            else
            {
                sb.AppendLine($"Locking processes ({results.Count}):");
                foreach (var lockInfo in results)
                {
                    sb.AppendLine($"  - {lockInfo.ProcessName} (PID {lockInfo.ProcessId})");
                    if (!string.IsNullOrEmpty(lockInfo.ExecutablePath))
                        sb.AppendLine($"    Path: {lockInfo.ExecutablePath}");
                    sb.AppendLine($"    Type: {lockInfo.ApplicationType}");
                    if (!string.IsNullOrEmpty(lockInfo.ServiceShortName))
                        sb.AppendLine($"    Service: {lockInfo.ServiceShortName}");
                    sb.AppendLine();
                }
            }

            if (filesScanned >= RestartManagerService.MaxFilesPerSession)
            {
                sb.AppendLine("Note: Scan limit reached. Not all files in the folder were checked.");
            }

            _detailTextBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            SetStatus($"Scan failed: {ex.Message}");
        }
        finally
        {
            Application.UseWaitCursor = false;
        }
    }

    private string GenerateReport()
    {
        var matched = _events.Where(e => e.MatchStatus == MatchStatus.Matched).ToList();
        var possible = _events.Where(e => e.MatchStatus == MatchStatus.PossiblyRelated).ToList();
        var unmatched = _events.Where(e => e.MatchStatus == MatchStatus.Unmatched).ToList();

        var model = new ReportModel
        {
            AppVersion = GetAppVersion(),
            WindowsVersion = Environment.OSVersion.VersionString,
            SelectedDrive = _selectedDrive?.DisplayText ?? "(none selected)",
            ScanTime = DateTime.Now,
            TimeRangeDescription = GetSelectedTimeRange().ToReportString(),
            MatchedEvents = matched,
            PossiblyRelatedEvents = possible,
            UnmatchedEvents = unmatched
        };

        return ReportService.GenerateReport(model);
    }

    private static string GetAppVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
        }
        catch
        {
            return "v1.0.0";
        }
    }

    private TimeRange GetSelectedTimeRange()
    {
        if (_timeRangeCombo.SelectedItem is TimeRangeItem item)
            return item.Range;
        return TimeRange.Hours2;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }
}

internal sealed class TimeRangeItem
{
    public TimeRange Range { get; }
    private readonly string _display;

    public TimeRangeItem(TimeRange range)
    {
        Range = range;
        _display = range.ToDisplayString();
    }

    public override string ToString() => _display;
}
