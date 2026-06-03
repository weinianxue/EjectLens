using System.Diagnostics;
using EjectLens.Models;
using EjectLens.Services;

namespace EjectLens;

public sealed partial class MainForm
{
    private void LoadDrives()
    {
        try
        {
            _drives = _driveService.GetRemovableDrives();

            _driveCombo.BeginUpdate();
            _driveCombo.Items.Clear();

            if (_drives.Count == 0)
            {
                _driveCombo.Items.Add(_loc.Get("NoDrives"));
                _driveCombo.SelectedIndex = 0;
                _selectedDrive = null;
                _ejectButton.Enabled = false;
            }
            else
            {
                int selectIndex = 0;
                for (int i = 0; i < _drives.Count; i++)
                {
                    _driveCombo.Items.Add(_drives[i]);
                    if (_settings.RememberLastDrive
                        && _drives[i].DriveLetter == _settings.LastSelectedDrive)
                        selectIndex = i;
                }
                _driveCombo.SelectedIndex = selectIndex;
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
            _ejectButton.Enabled = true;
            UpdateMatchStatus();
        }
        else
        {
            _selectedDrive = null;
            _ejectButton.Enabled = false;
        }
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        LoadDrives();
        SetStatus("Drive list refreshed.");
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings, _settingsService, ApplySettings);
        form.ShowDialog(this);
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

    private void EjectButton_Click(object? sender, EventArgs e)
    {
        if (_selectedDrive == null) return;

        if (_settings.ConfirmBeforeEject)
        {
            var result = MessageBox.Show(
                this,
                _loc.Get("EjectConfirmMessage"),
                _loc.Get("EjectConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;
        }

        SetStatus("Requesting safe eject...");
        Application.UseWaitCursor = true;

        try
        {
            _lastEjectResult = _ejectService.EjectDrive(_selectedDrive.DriveLetter + "\\");

            if (_lastEjectResult.Success)
            {
                SetStatus(_loc.Get("EjectSuccess"));
                _detailTextBox.Text = _lastEjectResult.ToReportText();

                if (_settings.RefreshAfterEject)
                    LoadDrives();
            }
            else
            {
                SetStatus(_lastEjectResult.Message);
                _detailTextBox.Text = _lastEjectResult.ToReportText();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Eject failed: {ex.Message}");
        }
        finally
        {
            Application.UseWaitCursor = false;
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
}
