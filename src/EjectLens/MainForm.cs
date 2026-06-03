using EjectLens.Models;
using EjectLens.Services;
using EjectLens.Utils;

namespace EjectLens;

public sealed partial class MainForm : Form
{
    // Services.
    private readonly RemovableDriveService _driveService = new();
    private readonly EventLogService _eventLogService = new();
    private readonly RestartManagerService _restartManagerService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DeviceEjectService _ejectService = new();
    private readonly LocalizationService _loc = new();

    // Settings.
    private AppSettings _settings;

    // Data.
    private List<DriveInfoModel> _drives = [];
    private List<EjectBlockEvent> _events = [];
    private DriveInfoModel? _selectedDrive;
    private EjectResult? _lastEjectResult;

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
    private readonly Button _settingsButton = new();
    private readonly Button _ejectButton = new();
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
        _settings = _settingsService.Load();
        _loc.SetLanguage(_settings.Language);

        InitializeForm();
        InitializeLayout();
        ApplyLocalization();
        ApplyTheme();
        LoadDrives();

        if (_settings.StartMaximized)
            WindowState = FormWindowState.Maximized;

        FormClosing += MainForm_FormClosing;
    }

    private void InitializeForm()
    {
        Text = "EjectLens — USB Safe Removal Diagnostics";
        var defaultSize = _settings.GetDefaultSize();
        Size = defaultSize;
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        if (_settings.RememberWindowSize
            && _settings.LastWindowWidth > 0
            && _settings.LastWindowHeight > 0)
        {
            Size = ClampToScreen(new Size(_settings.LastWindowWidth, _settings.LastWindowHeight));
            if (_settings.LastWindowX >= 0 && _settings.LastWindowY >= 0)
                Location = ClampToScreen(new Point(_settings.LastWindowX, _settings.LastWindowY));
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_settings.RememberWindowSize && WindowState == FormWindowState.Normal)
        {
            _settings.LastWindowWidth = Size.Width;
            _settings.LastWindowHeight = Size.Height;
            _settings.LastWindowX = Location.X;
            _settings.LastWindowY = Location.Y;
        }

        if (_settings.RememberLastDrive && _selectedDrive != null)
            _settings.LastSelectedDrive = _selectedDrive.DriveLetter;

        _settingsService.Save(_settings);
    }

    public void ApplySettings()
    {
        _settings = _settingsService.Load();
        _loc.SetLanguage(_settings.Language);
        ApplyLocalization();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var isDark = ShouldUseDarkMode();
        var backColor = isDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        var foreColor = isDark ? Color.FromArgb(240, 240, 240) : SystemColors.ControlText;
        var gridBack = isDark ? Color.FromArgb(45, 45, 48) : Color.White;
        var gridFore = isDark ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;

        BackColor = backColor;
        ForeColor = foreColor;

        _eventsGrid.BackgroundColor = isDark ? Color.FromArgb(37, 37, 38) : SystemColors.Window;
        _eventsGrid.DefaultCellStyle.BackColor = gridBack;
        _eventsGrid.DefaultCellStyle.ForeColor = gridFore;
        _eventsGrid.ColumnHeadersDefaultCellStyle.BackColor = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control;
        _eventsGrid.ColumnHeadersDefaultCellStyle.ForeColor = foreColor;
        _eventsGrid.EnableHeadersVisualStyles = !isDark;

        _detailTextBox.BackColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Window;
        _detailTextBox.ForeColor = foreColor;

        foreach (Control ctrl in _bottomPanel.Controls)
            ctrl.BackColor = backColor;
    }

    private bool ShouldUseDarkMode()
    {
        if (_settings.Theme == "dark") return true;
        if (_settings.Theme == "light") return false;
        try
        {
            const string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            var appsUseLight = Microsoft.Win32.Registry.GetValue(key, "AppsUseLightTheme", 1);
            return appsUseLight is 0;
        }
        catch { return false; }
    }

    private static Size ClampToScreen(Size size)
    {
        var screen = Screen.FromPoint(new Point(0, 0)).WorkingArea;
        return new Size(
            Math.Min(size.Width, screen.Width),
            Math.Min(size.Height, screen.Height));
    }

    private static Point ClampToScreen(Point point)
    {
        foreach (var screen in Screen.AllScreens)
        {
            if (screen.WorkingArea.Contains(point))
                return point;
        }
        return new Point(50, 50);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static string GetAppVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v0.2.0";
        }
        catch
        {
            return "v0.2.0";
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

    private void ApplyLocalization()
    {
        _refreshButton.Text = _loc.Get("Refresh");
        _scanEventsButton.Text = _loc.Get("ScanEvents");
        _copyReportButton.Text = _loc.Get("CopyReport");
        _saveReportButton.Text = _loc.Get("SaveReport");
        _openTaskManagerButton.Text = _loc.Get("OpenTaskManager");
        _scanFilesButton.Text = _loc.Get("ScanSelectedPath");
        _settingsButton.Text = _loc.Get("Settings");
        _ejectButton.Text = _loc.Get("EjectSelectedDrive");
        _detailLabel.Text = _loc.Get("EventDetails");

        UpdateDriveLabel();
        UpdateTimeLabel();
        UpdateGridHeaders();
    }

    private void UpdateDriveLabel()
    {
        if (_topPanel.Controls.Count > 0 && _topPanel.Controls[0] is Label driveLabel)
            driveLabel.Text = _loc.Get("Drive");
    }

    private void UpdateTimeLabel()
    {
        if (_topPanel.GetControlFromPosition(4, 0) is Label timeLabel)
            timeLabel.Text = _loc.Get("TimeRange");
    }

    private void UpdateGridHeaders()
    {
        if (_eventsGrid.Columns.Count < 8) return;
        _eventsGrid.Columns["Status"].HeaderText = _loc.Get("Status");
        _eventsGrid.Columns["Process"].HeaderText = _loc.Get("Process");
        _eventsGrid.Columns["PID"].HeaderText = _loc.Get("PID");
        _eventsGrid.Columns["Path"].HeaderText = _loc.Get("Path");
        _eventsGrid.Columns["CommandLine"].HeaderText = _loc.Get("CommandLine");
        _eventsGrid.Columns["AffectedDevice"].HeaderText = _loc.Get("AffectedDevice");
        _eventsGrid.Columns["State"].HeaderText = _loc.Get("State");
    }
}

/// <summary>
/// Wrapper for displaying TimeRange enum values in a ComboBox.
/// </summary>
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
