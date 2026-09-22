// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Singularity.UI.Controls;

public sealed class SingularityCheckBox : Control
{
	private bool isHovered;
	private bool isChecked;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Checked
	{
		get => isChecked;
		set
		{
			if (isChecked == value)
				return;

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
		TabStop = true;
		AccessibleRole = AccessibleRole.CheckButton;

		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.UserPaint |
			ControlStyles.OptimizedDoubleBuffer |
			ControlStyles.ResizeRedraw |
			ControlStyles.Selectable,
			true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

		Rectangle outer = new(0, 0, Width - 1, Height - 1);

		using SolidBrush backgroundBrush = new(Theme.Background);
		using SolidBrush hoverBrush = new(Color.FromArgb(48, 58, 78));
		using Pen borderPen = new(isChecked ? Theme.Accent : Color.FromArgb(80, 92, 115), 2);

		graphics.FillRectangle(isHovered ? hoverBrush : backgroundBrush, outer);
		graphics.DrawRectangle(borderPen, outer);

		if (isChecked)
		{
			using Pen checkPen = new(Theme.Accent, 3)
			{
				StartCap = System.Drawing.Drawing2D.LineCap.Round,
				EndCap = System.Drawing.Drawing2D.LineCap.Round
			};

			Point[] checkMark =
			[
				new Point(6, 14),
				new Point(11, 19),
				new Point(20, 8)
			];

			graphics.DrawLines(checkPen, checkMark);
		}

		if (Focused && ShowFocusCues && Width > 6 && Height > 6)
		{
			using Pen focusPen = new(Theme.Focus);
			graphics.DrawRectangle(focusPen, 3, 3, Width - 7, Height - 7);
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

	protected override void OnGotFocus(EventArgs e)
	{
		Invalidate();
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		Invalidate();
		base.OnLostFocus(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyCode is Keys.Space or Keys.Enter)
		{
			Checked = !Checked;
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		base.OnKeyDown(e);
	}

	protected override void OnClick(EventArgs e)
	{
		Focus();
		Checked = !Checked;
		base.OnClick(e);
	}
}
