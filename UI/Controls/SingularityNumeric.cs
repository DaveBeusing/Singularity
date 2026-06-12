namespace Singularity.UI.Controls;

/// <summary>
/// Eigenes numerisches Eingabefeld für Singularity.
/// Ersetzt das Standard-Windows-NumericUpDown-Control.
/// </summary>
public sealed class SingularityNumeric : Control
{
	private int value;
	private bool minusHover;
	private bool plusHover;
	public int Minimum { get; set; } = 0;
	public int Maximum { get; set; } = 100;
	public int Value
	{
		get => value;
		set
		{
			this.value = Math.Clamp(value, Minimum, Maximum);
			Invalidate();
		}
	}

	public event EventHandler? ValueChanged;

	public SingularityNumeric()
	{
		Width = 86;
		Height = 46;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = Theme.PanelLight;
		Cursor = Cursors.Hand;
		Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		Rectangle outer = new(0, 0, Width - 1, Height - 1);
		Rectangle valueArea = new(0, 0, Width - 1, 30);
		Rectangle minusArea = GetMinusArea();
		Rectangle plusArea = GetPlusArea();
		using SolidBrush backgroundBrush = new(Theme.Background);
		using SolidBrush hoverBrush = new(Color.FromArgb(48, 58, 78));
		using SolidBrush textBrush = new(Theme.TextMain);
		using SolidBrush mutedBrush = new(Theme.TextMuted);
		using Pen borderPen = new(Color.FromArgb(80, 92, 115));
		using Pen dividerPen = new(Color.FromArgb(60, 70, 90));
		g.FillRectangle(backgroundBrush, outer);
		g.DrawRectangle(borderPen, outer);
		g.FillRectangle(minusHover ? hoverBrush : backgroundBrush, minusArea);
		g.FillRectangle(plusHover ? hoverBrush : backgroundBrush, plusArea);
		g.DrawLine(dividerPen, 0, 30, Width, 30);
		g.DrawLine(dividerPen, Width / 2, 30, Width / 2, Height);
		DrawCenteredText(g, Value.ToString(), Font, textBrush, valueArea);
		using Font buttonFont = new("Segoe UI", 9F, FontStyle.Bold);
		DrawCenteredText(g, "−", buttonFont, mutedBrush, minusArea);
		DrawCenteredText(g, "+", buttonFont, mutedBrush, plusArea);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		bool oldMinus = minusHover;
		bool oldPlus = plusHover;
		minusHover = GetMinusArea().Contains(e.Location);
		plusHover = GetPlusArea().Contains(e.Location);
		if (oldMinus != minusHover || oldPlus != plusHover)
		{
			Invalidate();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		minusHover = false;
		plusHover = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (GetMinusArea().Contains(e.Location))
		{
			ChangeValue(-1);
		}
		else if (GetPlusArea().Contains(e.Location))
		{
			ChangeValue(1);
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		ChangeValue(e.Delta > 0 ? 1 : -1);
		base.OnMouseWheel(e);
	}

	private void ChangeValue(int delta)
	{
		int oldValue = Value;
		Value += delta;
		if (oldValue != Value)
		{
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private Rectangle GetMinusArea()
	{
		return new Rectangle(0, 30, Width / 2, Height - 30);
	}

	private Rectangle GetPlusArea()
	{
		return new Rectangle(Width / 2, 30, Width / 2, Height - 30);
	}

	private static void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle area)
	{
		using StringFormat format = new()
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		g.DrawString(text, font, brush, area, format);
	}

}