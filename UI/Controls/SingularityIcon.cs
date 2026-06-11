using Singularity.UI;

namespace Singularity.UI.Controls;

/// <summary>
/// Arten von Icons, die Singularity selbst zeichnet.
/// Keine externen Dateien, keine Icon-Library.
/// </summary>
public enum SingularityIconType
{
	Cpu,
	Memory,
	Gpu,
	Metrics,
	Play,
	Stop
}

/// <summary>
/// Kleines, selbst gezeichnetes Icon-Control.
/// Die Darstellung erfolgt über System.Drawing im OnPaint-Event.
/// </summary>
public sealed class SingularityIcon : Control
{
	public SingularityIconType IconType { get; set; }

	public Color IconColor { get; set; } = Color.White;

	public SingularityIcon()
	{
		Width = 32;
		Height = 32;

		// Verhindert Flackern beim Neuzeichnen.
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
		DoubleBuffered = true;

		// Das Control soll keinen sichtbaren Hintergrund erzwingen.
		//BackColor = Color.FromArgb(38, 46, 64);
		BackColor = Theme.PanelLight;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics g = e.Graphics;
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

		using Pen pen = new(IconColor, 2);
		using SolidBrush brush = new(IconColor);

		Rectangle area = new(4, 4, Width - 8, Height - 8);

		switch (IconType)
		{
			case SingularityIconType.Cpu:
				DrawCpu(g, pen);
				break;

			case SingularityIconType.Memory:
				DrawMemory(g, pen);
				break;

			case SingularityIconType.Gpu:
				DrawGpu(g, pen);
				break;

			case SingularityIconType.Metrics:
				DrawMetrics(g, pen);
				break;

			case SingularityIconType.Play:
				DrawPlay(g, brush);
				break;

			case SingularityIconType.Stop:
				g.FillRectangle(brush, area);
				break;
		}
	}

	private static void DrawCpu(Graphics g, Pen pen)
	{
		Rectangle chip = new(9, 9, 14, 14);
		g.DrawRectangle(pen, chip);

		for (int i = 0; i < 4; i++)
		{
			int offset = 8 + i * 5;

			g.DrawLine(pen, offset, 4, offset, 8);
			g.DrawLine(pen, offset, 24, offset, 28);
			g.DrawLine(pen, 4, offset, 8, offset);
			g.DrawLine(pen, 24, offset, 28, offset);
		}

		g.DrawRectangle(pen, 13, 13, 6, 6);
	}

	private static void DrawMemory(Graphics g, Pen pen)
	{
		Rectangle body = new(5, 10, 22, 12);
		g.DrawRectangle(pen, body);

		for (int i = 0; i < 4; i++)
		{
			int x = 8 + i * 5;
			g.DrawRectangle(pen, x, 13, 3, 5);
		}

		for (int i = 0; i < 6; i++)
		{
			int x = 6 + i * 4;
			g.DrawLine(pen, x, 22, x, 26);
		}
	}

	private static void DrawGpu(Graphics g, Pen pen)
	{
		Rectangle card = new(5, 9, 22, 14);
		g.DrawRectangle(pen, card);

		g.DrawEllipse(pen, 9, 12, 7, 7);
		g.DrawEllipse(pen, 16, 12, 7, 7);

		g.DrawLine(pen, 6, 24, 12, 24);
		g.DrawLine(pen, 20, 24, 26, 24);

		g.DrawLine(pen, 27, 13, 30, 13);
		g.DrawLine(pen, 27, 17, 30, 17);
	}

	private static void DrawMetrics(Graphics g, Pen pen)
	{
		g.DrawLine(pen, 6, 25, 26, 25);
		g.DrawLine(pen, 6, 25, 6, 6);

		g.DrawLine(pen, 9, 21, 14, 16);
		g.DrawLine(pen, 14, 16, 18, 18);
		g.DrawLine(pen, 18, 18, 25, 10);

		g.DrawLine(pen, 22, 10, 25, 10);
		g.DrawLine(pen, 25, 10, 25, 13);
	}

	private static void DrawPlay(Graphics g, SolidBrush brush)
	{
		Point[] points =
		[
			new Point(9, 6),
			new Point(25, 16),
			new Point(9, 26)
		];

		g.FillPolygon(brush, points);
	}
}