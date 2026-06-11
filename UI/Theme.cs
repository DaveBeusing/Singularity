namespace Singularity.UI;

/// <summary>
/// Zentrale Farbdefinitionen für die gesamte Oberfläche.
/// Dadurch müssen Farben später nur noch an einer Stelle geändert werden.
/// </summary>
public static class Theme
{
	public static readonly Color Background = Color.FromArgb(18, 22, 32);

	public static readonly Color Panel = Color.FromArgb(28, 34, 48);

	public static readonly Color PanelLight = Color.FromArgb(38, 46, 64);

	public static readonly Color Accent = Color.FromArgb(255, 192, 0);

	public static readonly Color TextMain = Color.FromArgb(240, 244, 248);

	public static readonly Color TextMuted = Color.FromArgb(150, 160, 175);

	public static readonly Color Danger = Color.FromArgb(220, 70, 70);

	public static readonly Color Success = Color.FromArgb(80, 200, 120);
}