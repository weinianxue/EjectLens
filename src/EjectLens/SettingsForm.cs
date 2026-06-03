using EjectLens.Models;
using EjectLens.Services;

namespace EjectLens;

/// <summary>
/// Application settings dialog with tabs for General, Appearance, and Behavior.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly Action _onSettingsChanged;

    private readonly TabControl _tabControl = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _applyButton = new();

    private readonly ComboBox _languageCombo = new();
    private readonly ComboBox _themeCombo = new();
    private readonly ComboBox _windowSizeCombo = new();
    private readonly CheckBox _startMaximizedCheck = new();
    private readonly CheckBox _rememberSizeCheck = new();
    private readonly CheckBox _confirmEjectCheck = new();
    private readonly CheckBox _refreshAfterEjectCheck = new();
    private readonly CheckBox _rememberDriveCheck = new();
    private readonly ComboBox _defaultTimeRangeCombo = new();

    public SettingsForm(AppSettings settings, SettingsService settingsService, Action onSettingsChanged)
    {
        _settings = settings;
        _settingsService = settingsService;
        _onSettingsChanged = onSettingsChanged;

        Text = "EjectLens Settings";
        Size = new Size(500, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        LoadSettingsToUi();
    }

    private void BuildUi()
    {
        _tabControl.Dock = DockStyle.Top;
        _tabControl.Height = 300;
        _tabControl.TabPages.Add(BuildGeneralTab());
        _tabControl.TabPages.Add(BuildAppearanceTab());
        _tabControl.TabPages.Add(BuildBehaviorTab());

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            Height = 44
        };

        _okButton.Text = "OK";
        _okButton.Width = 80;
        _okButton.Click += OkButton_Click;

        _cancelButton.Text = "Cancel";
        _cancelButton.Width = 80;
        _cancelButton.Click += CancelButton_Click;

        _applyButton.Text = "Apply";
        _applyButton.Width = 80;
        _applyButton.Click += ApplyButton_Click;

        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_applyButton);

        Controls.Add(_tabControl);
        Controls.Add(buttonPanel);
    }

    private TabPage BuildGeneralTab()
    {
        var page = new TabPage("General");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        var langLabel = new Label
        {
            Text = "Language:",
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Right
        };
        table.Controls.Add(langLabel, 0, 0);

        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.Items.Add("System default");
        _languageCombo.Items.Add("English");
        _languageCombo.Items.Add("简体中文");
        _languageCombo.Anchor = AnchorStyles.Left;
        _languageCombo.Width = 160;
        table.Controls.Add(_languageCombo, 1, 0);

        page.Controls.Add(table);
        return page;
    }

    private TabPage BuildAppearanceTab()
    {
        var page = new TabPage("Appearance");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        table.Controls.Add(MakeLabel("Theme:"), 0, 0);
        _themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeCombo.Items.Add("System");
        _themeCombo.Items.Add("Light");
        _themeCombo.Items.Add("Dark");
        _themeCombo.Anchor = AnchorStyles.Left;
        _themeCombo.Width = 120;
        table.Controls.Add(_themeCombo, 1, 0);

        table.Controls.Add(MakeLabel("Default size:"), 0, 1);
        _windowSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _windowSizeCombo.Items.Add("Small (900x600)");
        _windowSizeCombo.Items.Add("Medium (1100x700)");
        _windowSizeCombo.Items.Add("Large (1300x850)");
        _windowSizeCombo.Anchor = AnchorStyles.Left;
        _windowSizeCombo.Width = 160;
        table.Controls.Add(_windowSizeCombo, 1, 1);

        table.Controls.Add(MakeLabel("Remember size:"), 0, 2);
        _rememberSizeCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_rememberSizeCheck, 1, 2);

        table.Controls.Add(MakeLabel("Start maximized:"), 0, 3);
        _startMaximizedCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_startMaximizedCheck, 1, 3);

        page.Controls.Add(table);
        return page;
    }

    private TabPage BuildBehaviorTab()
    {
        var page = new TabPage("Behavior");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

        table.Controls.Add(MakeLabel("Confirm before eject:"), 0, 0);
        _confirmEjectCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_confirmEjectCheck, 1, 0);

        table.Controls.Add(MakeLabel("Refresh after eject:"), 0, 1);
        _refreshAfterEjectCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_refreshAfterEjectCheck, 1, 1);

        table.Controls.Add(MakeLabel("Remember last drive:"), 0, 2);
        _rememberDriveCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_rememberDriveCheck, 1, 2);

        table.Controls.Add(MakeLabel("Default time range:"), 0, 3);
        _defaultTimeRangeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultTimeRangeCombo.Items.Add("15 minutes");
        _defaultTimeRangeCombo.Items.Add("1 hour");
        _defaultTimeRangeCombo.Items.Add("2 hours");
        _defaultTimeRangeCombo.Items.Add("24 hours");
        _defaultTimeRangeCombo.Anchor = AnchorStyles.Left;
        _defaultTimeRangeCombo.Width = 140;
        table.Controls.Add(_defaultTimeRangeCombo, 1, 3);

        page.Controls.Add(table);
        return page;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        TextAlign = ContentAlignment.MiddleRight,
        Anchor = AnchorStyles.Right,
        AutoSize = true
    };

    private void LoadSettingsToUi()
    {
        _languageCombo.SelectedIndex = _settings.Language switch
        {
            "zh-CN" => 2,
            "en-US" => 1,
            _ => 0
        };

        _themeCombo.SelectedIndex = _settings.Theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };
        _windowSizeCombo.SelectedIndex = _settings.DefaultWindowSize switch
        {
            "small" => 0,
            "large" => 2,
            _ => 1
        };
        _rememberSizeCheck.Checked = _settings.RememberWindowSize;
        _startMaximizedCheck.Checked = _settings.StartMaximized;

        _confirmEjectCheck.Checked = _settings.ConfirmBeforeEject;
        _refreshAfterEjectCheck.Checked = _settings.RefreshAfterEject;
        _rememberDriveCheck.Checked = _settings.RememberLastDrive;
        _defaultTimeRangeCombo.SelectedIndex = _settings.DefaultTimeRange switch
        {
            "15min" => 0,
            "1hour" => 1,
            "24hours" => 3,
            _ => 2
        };
    }

    private void SaveUiToSettings()
    {
        _settings.Language = _languageCombo.SelectedIndex switch
        {
            2 => "zh-CN",
            1 => "en-US",
            _ => "system"
        };

        _settings.Theme = _themeCombo.SelectedIndex switch
        {
            1 => "light",
            2 => "dark",
            _ => "system"
        };
        _settings.DefaultWindowSize = _windowSizeCombo.SelectedIndex switch
        {
            0 => "small",
            2 => "large",
            _ => "medium"
        };
        _settings.RememberWindowSize = _rememberSizeCheck.Checked;
        _settings.StartMaximized = _startMaximizedCheck.Checked;

        _settings.ConfirmBeforeEject = _confirmEjectCheck.Checked;
        _settings.RefreshAfterEject = _refreshAfterEjectCheck.Checked;
        _settings.RememberLastDrive = _rememberDriveCheck.Checked;
        _settings.DefaultTimeRange = _defaultTimeRangeCombo.SelectedIndex switch
        {
            0 => "15min",
            1 => "1hour",
            3 => "24hours",
            _ => "2hours"
        };
    }

    private void ApplyButton_Click(object? sender, EventArgs e)
    {
        SaveUiToSettings();
        _settingsService.Save(_settings);
        _onSettingsChanged();
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        SaveUiToSettings();
        _settingsService.Save(_settings);
        _onSettingsChanged();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
