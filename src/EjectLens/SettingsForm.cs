using EjectLens.Models;
using EjectLens.Services;

namespace EjectLens;

/// <summary>
/// Application settings dialog with tabs for General, Appearance, and Behavior.
/// Uses LocalizationService for all visible labels.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _loc;
    private readonly Action _onSettingsChanged;

    private readonly TabControl _tabControl = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _applyButton = new();

    private readonly TabPage _tabGeneral = new();
    private readonly TabPage _tabAppearance = new();
    private readonly TabPage _tabBehavior = new();
    private readonly Label _langLabel = new();
    private readonly Label _themeLabel = new();
    private readonly Label _sizeLabel = new();
    private readonly Label _rememberSizeLabel = new();
    private readonly Label _startMaxLabel = new();
    private readonly Label _confirmEjectLabel = new();
    private readonly Label _refreshAfterEjectLabel = new();
    private readonly Label _rememberDriveLabel = new();
    private readonly Label _defaultTimeRangeLabel = new();

    private readonly ComboBox _languageCombo = new();
    private readonly ComboBox _themeCombo = new();
    private readonly ComboBox _windowSizeCombo = new();
    private readonly CheckBox _startMaximizedCheck = new();
    private readonly CheckBox _rememberSizeCheck = new();
    private readonly CheckBox _confirmEjectCheck = new();
    private readonly CheckBox _refreshAfterEjectCheck = new();
    private readonly CheckBox _rememberDriveCheck = new();
    private readonly ComboBox _defaultTimeRangeCombo = new();

    public SettingsForm(AppSettings settings, SettingsService settingsService,
        LocalizationService loc, Action onSettingsChanged)
    {
        _settings = settings;
        _settingsService = settingsService;
        _loc = loc;
        _onSettingsChanged = onSettingsChanged;

        Text = _loc.Get("SettingsTitle");
        Size = new Size(500, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        ApplyLocalization();
        LoadSettingsToUi();
    }

    private void BuildUi()
    {
        _tabControl.Dock = DockStyle.Top;
        _tabControl.Height = 300;

        BuildGeneralTab();
        BuildAppearanceTab();
        BuildBehaviorTab();

        _tabControl.TabPages.Add(_tabGeneral);
        _tabControl.TabPages.Add(_tabAppearance);
        _tabControl.TabPages.Add(_tabBehavior);

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

    private void BuildGeneralTab()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        _langLabel.Text = "Language:";
        _langLabel.TextAlign = ContentAlignment.MiddleRight;
        _langLabel.Anchor = AnchorStyles.Right;
        table.Controls.Add(_langLabel, 0, 0);

        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.Anchor = AnchorStyles.Left;
        _languageCombo.Width = 160;
        table.Controls.Add(_languageCombo, 1, 0);

        _tabGeneral.Controls.Add(table);
    }

    private void BuildAppearanceTab()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        table.Controls.Add(MakeLabelRef(_themeLabel), 0, 0);
        _themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeCombo.Anchor = AnchorStyles.Left;
        _themeCombo.Width = 120;
        table.Controls.Add(_themeCombo, 1, 0);

        table.Controls.Add(MakeLabelRef(_sizeLabel), 0, 1);
        _windowSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _windowSizeCombo.Anchor = AnchorStyles.Left;
        _windowSizeCombo.Width = 160;
        table.Controls.Add(_windowSizeCombo, 1, 1);

        table.Controls.Add(MakeLabelRef(_rememberSizeLabel), 0, 2);
        _rememberSizeCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_rememberSizeCheck, 1, 2);

        table.Controls.Add(MakeLabelRef(_startMaxLabel), 0, 3);
        _startMaximizedCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_startMaximizedCheck, 1, 3);

        _tabAppearance.Controls.Add(table);
    }

    private void BuildBehaviorTab()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12, 12, 12, 12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

        table.Controls.Add(MakeLabelRef(_confirmEjectLabel), 0, 0);
        _confirmEjectCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_confirmEjectCheck, 1, 0);

        table.Controls.Add(MakeLabelRef(_refreshAfterEjectLabel), 0, 1);
        _refreshAfterEjectCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_refreshAfterEjectCheck, 1, 1);

        table.Controls.Add(MakeLabelRef(_rememberDriveLabel), 0, 2);
        _rememberDriveCheck.Anchor = AnchorStyles.Left;
        table.Controls.Add(_rememberDriveCheck, 1, 2);

        table.Controls.Add(MakeLabelRef(_defaultTimeRangeLabel), 0, 3);
        _defaultTimeRangeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultTimeRangeCombo.Anchor = AnchorStyles.Left;
        _defaultTimeRangeCombo.Width = 140;
        table.Controls.Add(_defaultTimeRangeCombo, 1, 3);

        _tabBehavior.Controls.Add(table);
    }

    private static Label MakeLabelRef(Label label)
    {
        label.TextAlign = ContentAlignment.MiddleRight;
        label.Anchor = AnchorStyles.Right;
        label.AutoSize = true;
        return label;
    }

    private void ApplyLocalization()
    {
        Text = _loc.Get("SettingsTitle");
        _tabGeneral.Text = _loc.Get("TabGeneral");
        _tabAppearance.Text = _loc.Get("TabAppearance");
        _tabBehavior.Text = _loc.Get("TabBehavior");

        _langLabel.Text = _loc.Get("SettingsLanguage");
        _themeLabel.Text = _loc.Get("SettingsTheme");
        _sizeLabel.Text = _loc.Get("SettingsDefaultSize");
        _rememberSizeLabel.Text = _loc.Get("SettingsRememberSize");
        _startMaxLabel.Text = _loc.Get("SettingsStartMaximized");
        _confirmEjectLabel.Text = _loc.Get("SettingsConfirmEject");
        _refreshAfterEjectLabel.Text = _loc.Get("SettingsRefreshAfterEject");
        _rememberDriveLabel.Text = _loc.Get("SettingsRememberDrive");
        _defaultTimeRangeLabel.Text = _loc.Get("SettingsDefaultTimeRange");

        PopulateLanguageCombo();
        PopulateThemeCombo();
        PopulateSizeCombo();
        PopulateTimeRangeCombo();
    }

    private void PopulateLanguageCombo()
    {
        var idx = _languageCombo.SelectedIndex;
        _languageCombo.Items.Clear();
        _languageCombo.Items.Add(_loc.Get("LangSystemDefault"));
        _languageCombo.Items.Add(_loc.Get("LangEnglish"));
        _languageCombo.Items.Add(_loc.Get("LangChinese"));
        _languageCombo.SelectedIndex = idx >= 0 && idx < 3 ? idx : 0;
    }

    private void PopulateThemeCombo()
    {
        var idx = _themeCombo.SelectedIndex;
        _themeCombo.Items.Clear();
        _themeCombo.Items.Add(_loc.Get("ThemeSystem"));
        _themeCombo.Items.Add(_loc.Get("ThemeLight"));
        _themeCombo.Items.Add(_loc.Get("ThemeDark"));
        _themeCombo.SelectedIndex = idx >= 0 && idx < 3 ? idx : 0;
    }

    private void PopulateSizeCombo()
    {
        var idx = _windowSizeCombo.SelectedIndex;
        _windowSizeCombo.Items.Clear();
        _windowSizeCombo.Items.Add(_loc.Get("SizeSmall"));
        _windowSizeCombo.Items.Add(_loc.Get("SizeMedium"));
        _windowSizeCombo.Items.Add(_loc.Get("SizeLarge"));
        _windowSizeCombo.SelectedIndex = idx >= 0 && idx < 3 ? idx : 0;
    }

    private void PopulateTimeRangeCombo()
    {
        var idx = _defaultTimeRangeCombo.SelectedIndex;
        _defaultTimeRangeCombo.Items.Clear();
        _defaultTimeRangeCombo.Items.Add(_loc.Get("Time15min"));
        _defaultTimeRangeCombo.Items.Add(_loc.Get("Time1hour"));
        _defaultTimeRangeCombo.Items.Add(_loc.Get("Time2hours"));
        _defaultTimeRangeCombo.Items.Add(_loc.Get("Time24hours"));
        _defaultTimeRangeCombo.SelectedIndex = idx >= 0 && idx < 4 ? idx : 2;
    }

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
