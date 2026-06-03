using System.Runtime.InteropServices;

namespace EjectLens;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Enable visual styles for modern Windows appearance.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.Run(new MainForm());
    }
}
