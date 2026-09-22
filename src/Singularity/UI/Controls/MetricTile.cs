// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public sealed class MetricTile : Panel
{
	private readonly Label valueLabel = new();
	private readonly Label detailLabel = new();

	public MetricTile(string title)
	{
		Height = 96;
		Width = 240;
		BackColor = Theme.PanelLight;
		Padding = new Padding(ThemeMetrics.Spacing);

		Label titleLabel = new()
		{
			Dock = DockStyle.Top,
			Height = 20,
			Text = title.ToUpperInvariant(),
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};

		valueLabel.Dock = DockStyle.Top;
		valueLabel.Height = 32;
		valueLabel.Font = ThemeFonts.MetricValue;
		valueLabel.ForeColor = Theme.TextMain;
		valueLabel.BackColor = Theme.PanelLight;
		valueLabel.TextAlign = ContentAlignment.MiddleLeft;
		valueLabel.AutoEllipsis = true;

		detailLabel.Dock = DockStyle.Fill;
		detailLabel.Font = ThemeFonts.CardTextSmall;
		detailLabel.ForeColor = Theme.TextMuted;
		detailLabel.BackColor = Theme.PanelLight;
		detailLabel.TextAlign = ContentAlignment.TopLeft;
		detailLabel.AutoEllipsis = true;

		Controls.Add(detailLabel);
		Controls.Add(valueLabel);
		Controls.Add(titleLabel);
	}

	public void SetValue(string value, string detail = "", Color? valueColor = null)
	{
		ControlUpdate.SetText(valueLabel, value);
		ControlUpdate.SetText(detailLabel, detail);
		ControlUpdate.SetForeColor(valueLabel, valueColor ?? Theme.TextMain);
	}
}
