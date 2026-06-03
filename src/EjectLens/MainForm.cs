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

    // ── Helpers ─────────────────────────────────────────────────

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
