// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public sealed class DeviceSelectionCard : Panel
{
	private bool selected;

	public DeviceSelectionCard(Control content)
	{
		ArgumentNullException.ThrowIfNull(content);

		BackColor = Theme.PanelLight;
		TabStop = true;
		Cursor = Cursors.Hand;
		Padding = new Padding(2);
		Width = content.Width + Padding.Horizontal;
		Height = content.Height + Padding.Vertical;

		content.Dock = DockStyle.Fill;
		Controls.Add(content);
		WireClick(content);
	}

	public event Action? SelectionRequested;

	public bool Selected
	{
		get => selected;
		set
		{
			if (selected == value)
				return;

			selected = value;
			Invalidate();
		}
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		RequestSelection();
	}

	protected override void OnEnter(EventArgs e)
	{
		base.OnEnter(e);
		Invalidate();
	}

	protected override void OnLeave(EventArgs e)
	{
		base.OnLeave(e);
		Invalidate();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.KeyCode is Keys.Enter or Keys.Space)
		{
			RequestSelection();
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Color borderColor = Selected ? Theme.Accent : Theme.PanelLight;
		using Pen border = new(borderColor, Selected ? 2F : 1F);
		e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

		if (Focused && ShowFocusCues && Width > 5 && Height > 5)
		{
			using Pen focus = new(Theme.Focus);
			e.Graphics.DrawRectangle(focus, 2, 2, Width - 5, Height - 5);
		}
	}

	private void WireClick(Control control)
	{
		control.Cursor = Cursors.Hand;
		control.Click += (_, _) => RequestSelection();

		foreach (Control child in control.Controls)
			WireClick(child);
	}

	private void RequestSelection()
	{
		Focus();
		SelectionRequested?.Invoke();
	}
}
