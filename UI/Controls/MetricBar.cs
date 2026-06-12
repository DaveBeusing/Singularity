namespace Singularity.UI.Controls;

/// <summary>
/// Eigene Progress-Bar für Singularity, die WinForms sieht zu altbacken aus.
/// Wird komplett selbst gezeichnet, so bleiben wir flexibel.
/// </summary>
public sealed class MetricBar : Control
	{
	private int value;

	/// <summary>
	/// Prozentwert von 0 bis 100.
	/// </summary>
	public int Value
	{
		get => value;
		set
		{
			this.value = Math.Clamp(value, 0, 100);
			Invalidate();
		}
	}

	public Color FillColor { get; set; } = Theme.Accent;

	public MetricBar()
	{
		Height = 10;

		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.UserPaint |
			ControlStyles.OptimizedDoubleBuffer,
			true);

		BackColor = Theme.PanelLight;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics g = e.Graphics;

		g.SmoothingMode =
			System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

		Rectangle background =
			new(0, 0, Width - 1, Height - 1);

		using SolidBrush backgroundBrush =
			new(Theme.Background);

		g.FillRectangle(backgroundBrush, background);

		int fillWidth =
			(int)((Width - 1) * (Value / 100.0));

		Rectangle fill =
			new(0, 0, fillWidth, Height - 1);

		using SolidBrush fillBrush =
			new(FillColor);

		g.FillRectangle(fillBrush, fill);
	}
}