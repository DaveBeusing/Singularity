//Klassen einbinden
using Singularity.UI;

namespace Singularity;


/// <summary>
/// Einstiegspunkt der Anwendung.
/// Hier wird die Windows-Forms-Anwendung initialisiert und das Hauptfenster gestartet.
/// </summary>
internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}
}