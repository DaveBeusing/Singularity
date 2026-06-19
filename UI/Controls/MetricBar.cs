// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Singularity.UI.Controls;

/// <summary>
/// Eigene Progress-Bar für Singularity.
/// Wird komplett selbst gezeichnet.
/// </summary>
public sealed class MetricBar : Control
{
	private int value;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int Value
	{
		get => value;
		set
		{
			this.value = Math.Clamp(value, 0, 100);
			Invalidate();
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Color FillColor { get; set; } = Theme.Accent;

	public MetricBar()
	{
		Height = 10;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
		BackColor = Theme.PanelLight;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics g = e.Graphics;
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

		Rectangle background = new(0, 0, Width - 1, Height - 1);

		using SolidBrush backgroundBrush = new(Theme.Background);
		g.FillRectangle(backgroundBrush, background);

		int fillWidth = (int)((Width - 1) * (Value / 100.0));

		Rectangle fill = new(0, 0, fillWidth, Height - 1);

		using SolidBrush fillBrush = new(FillColor);
		g.FillRectangle(fillBrush, fill);
	}

}