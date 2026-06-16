// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class MetricsPanel : Panel
{
	public Label ValueLabel { get; } = new();
	public MetricBar Bar { get; } = new();

	public MetricsPanel(string title, Color barColor, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Label titleLabel = new()
		{
			Text = title,
			Left = 18,
			Top = 12,
			Width = width - 36,
			Height = 20,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		ValueLabel.Left = 18;
		ValueLabel.Top = 32;
		ValueLabel.Width = width - 45;
		ValueLabel.Height = 30;
		ValueLabel.Font =  ThemeFonts.MetricValue;
		ValueLabel.ForeColor = Theme.TextMain;
		ValueLabel.BackColor = Theme.PanelLight;
		ValueLabel.Text = "-";
		ValueLabel.AutoEllipsis = true;

		Bar.Left = 18;
		Bar.Top = 64;
		Bar.Width = width - 36;
		Bar.Height = 7;
		Bar.FillColor = barColor;
		Bar.BackColor = Theme.PanelLight;

		Controls.Add(titleLabel);
		Controls.Add(ValueLabel);
		Controls.Add(Bar);
	}

}