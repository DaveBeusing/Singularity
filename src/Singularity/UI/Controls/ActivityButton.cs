// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Singularity.UI.Controls;

public sealed class ActivityButton : CommandButton
{
	private bool selected;

	public ActivityButton()
	{
		Width = ThemeMetrics.ActivityButtonSize;
		Height = ThemeMetrics.ActivityButtonSize;
		Margin = Padding.Empty;
		Padding = Padding.Empty;
		TextAlign = ContentAlignment.MiddleCenter;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Selected
	{
		get => selected;
		set
		{
			if (selected == value)
				return;

			selected = value;
			BackColor = selected ? Theme.Selected : Theme.ActivityBar;
			ForeColor = selected ? Theme.TextMain : Theme.TextMuted;
			Invalidate();
		}
	}

	protected override void OnPaint(PaintEventArgs pevent)
	{
		base.OnPaint(pevent);

		if (!selected)
			return;

		using SolidBrush brush = new(Theme.Accent);
		pevent.Graphics.FillRectangle(brush, 0, 5, 3, Math.Max(0, Height - 10));
	}
}
