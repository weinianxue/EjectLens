namespace EjectLens.Services;

/// <summary>
/// Provides localized strings for the UI.
/// Supports en-US, zh-CN, and system default (follows OS language).
/// Falls back to en-US for any unknown keys.
/// </summary>
public class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["en-US"] = new Dictionary<string, string>
        {
            ["Drive"] = "Drive:",
            ["Refresh"] = "Refresh",
            ["TimeRange"] = "Time range:",
            ["ScanEvents"] = "Scan Events",
            ["CopyReport"] = "Copy Report",
            ["SaveReport"] = "Save Report",
            ["OpenTaskManager"] = "Open Task Manager",
            ["ScanSelectedPath"] = "Scan Selected Path...",
            ["Settings"] = "Settings",
            ["EjectSelectedDrive"] = "Eject Selected Drive",
            ["EventDetails"] = "Event Details",
            ["Status"] = "Status",
            ["Process"] = "Process",
            ["PID"] = "PID",
            ["Path"] = "Path",
            ["CommandLine"] = "Command Line",
            ["AffectedDevice"] = "Affected Device",
            ["State"] = "State",
            ["NoDrives"] = "(no removable drives found)",
            ["EjectConfirmTitle"] = "Safe Eject Confirmation",
            ["EjectConfirmMessage"] = "This asks Windows to safely eject the selected device. Save your files and close applications first. Continue?",
            ["EjectSuccess"] = "Windows reported that the device can be safely removed.",
            ["EjectFailed"] = "Could not safely eject the device.",
            ["EjectVetoed"] = "Windows rejected the eject request. Check for open files or applications using the drive.",
            ["EjectNotResolved"] = "Could not resolve the selected drive to a removable device instance.",
            ["EjectNeedAdmin"] = "Administrator privileges may be required to eject this device.",
        },
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["Drive"] = "驱动器:",
            ["Refresh"] = "刷新",
            ["TimeRange"] = "时间范围:",
            ["ScanEvents"] = "扫描事件",
            ["CopyReport"] = "复制报告",
            ["SaveReport"] = "保存报告",
            ["OpenTaskManager"] = "打开任务管理器",
            ["ScanSelectedPath"] = "扫描所选路径...",
            ["Settings"] = "设置",
            ["EjectSelectedDrive"] = "安全弹出所选设备",
            ["EventDetails"] = "事件详情",
            ["Status"] = "状态",
            ["Process"] = "进程",
            ["PID"] = "PID",
            ["Path"] = "路径",
            ["CommandLine"] = "命令行",
            ["AffectedDevice"] = "受影响设备",
            ["State"] = "运行状态",
            ["NoDrives"] = "(未找到可移动磁盘)",
            ["EjectConfirmTitle"] = "安全弹出确认",
            ["EjectConfirmMessage"] = "将请求 Windows 安全弹出所选设备。请先保存文件并关闭相关程序。要继续吗？",
            ["EjectSuccess"] = "Windows 已报告该设备可以安全移除。",
            ["EjectFailed"] = "无法安全弹出该设备。",
            ["EjectVetoed"] = "Windows 拒绝了弹出请求。请检查是否有程序正在使用该磁盘。",
            ["EjectNotResolved"] = "无法将所选磁盘解析为可移动设备实例。",
            ["EjectNeedAdmin"] = "弹出该设备可能需要管理员权限。",
        }
    };

    private string _language = "en-US";

    /// <summary>
    /// Set the active language. Use "system" to follow the OS locale,
    /// "en-US" for English, "zh-CN" for Simplified Chinese.
    /// </summary>
    public void SetLanguage(string language)
    {
        if (language == "system")
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            _language = culture.Name.StartsWith("zh") ? "zh-CN" : "en-US";
        }
        else if (_strings.ContainsKey(language))
        {
            _language = language;
        }
        else
        {
            _language = "en-US";
        }
    }

    /// <summary>
    /// Get a localized string by key. Falls back to en-US for unknown keys.
    /// </summary>
    public string Get(string key)
    {
        if (_strings.TryGetValue(_language, out var langStrings)
            && langStrings.TryGetValue(key, out var value))
            return value;

        // Fallback to en-US.
        if (_strings.TryGetValue("en-US", out var enStrings)
            && enStrings.TryGetValue(key, out var enValue))
            return enValue;

        return key;
    }

    /// <summary>
    /// Returns the current language code (e.g. "en-US", "zh-CN").
    /// </summary>
    public string CurrentLanguage => _language;

    public bool IsZhCn => _language == "zh-CN";
}
