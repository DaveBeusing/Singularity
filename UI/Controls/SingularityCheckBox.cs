namespace Singularity.UI.Controls;

/// <summary>
/// Eigene Checkbox für Singularity.
/// Zeichnet sich selbst und ersetzt die Standard-Windows-Checkbox.
/// </summary>
public sealed class SingularityCheckBox : Control
{
	private bool isHovered;
	private bool isChecked;

	public bool Checked
	{
		get => isChecked;
		set
		{
			isChecked = value;
			Invalidate();
			CheckedChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? CheckedChanged;

	public SingularityCheckBox()
	{
		Width = 30;
		Height = 30;
		Cursor = Cursors.Hand;
		BackColor = Theme.PanelLight;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		Rectangle outer = new(0, 0, Width - 1, Height - 1);
		using SolidBrush backgroundBrush = new(Theme.Background);
		using SolidBrush hoverBrush = new(Color.FromArgb(48, 58, 78));
		// w/ border highlight
		using Pen borderPen = new(isChecked ? Theme.Accent : Color.FromArgb(80, 92, 115), 2);
		// w/o border highlight
		//using Pen borderPen = new(Color.FromArgb(80, 92, 115));
		g.FillRectangle(isHovered ? hoverBrush : backgroundBrush, outer);
		g.DrawRectangle(borderPen, outer);
		if (isChecked)
		{
			using Pen checkPen = new(Theme.Accent, 3)
			{
				StartCap = System.Drawing.Drawing2D.LineCap.Round,
				EndCap = System.Drawing.Drawing2D.LineCap.Round
			};
			Point[] checkMark =
			{
				new Point(6, 14),
				new Point(11, 19),
				new Point(20, 8)
			};
			g.DrawLines(checkPen, checkMark);
		}
	}
	protected override void OnMouseEnter(EventArgs e)
	{
		isHovered = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		isHovered = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnClick(EventArgs e)
	{
		Checked = !Checked;
		base.OnClick(e);
	}

}