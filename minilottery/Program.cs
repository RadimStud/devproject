using System.Globalization;

namespace WinFormsApp1;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("cs-CZ");
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
