using EjectLens.Models;
using EjectLens.Services;
using EjectLens.Utils;

namespace EjectLens;

public sealed partial class MainForm
{
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

        var report = ReportService.GenerateReport(model);

        if (_lastEjectResult != null)
        {
            report += Environment.NewLine
                + "── Eject Attempt ──" + Environment.NewLine
                + Environment.NewLine
                + _lastEjectResult.ToReportText()
                + Environment.NewLine;
        }

        return report;
    }
}
