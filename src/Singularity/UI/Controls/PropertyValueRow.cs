// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public sealed class PropertyValueRow : TableLayoutPanel
{
	private readonly Label valueLabel = new();

	public PropertyValueRow(string name, string value)
	{
		Height = 30;
		Dock = DockStyle.Top;
		BackColor = Theme.Panel;
		ColumnCount = 2;
		RowCount = 1;
		Margin = Padding.Empty;
		Padding = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall);
		ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
		ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

		Label nameLabel = new()
		{
			Dock = DockStyle.Fill,
			Text = name,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true
		};

		valueLabel.Dock = DockStyle.Fill;
		valueLabel.Text = value;
		valueLabel.Font = ThemeFonts.CardText;
		valueLabel.ForeColor = Theme.TextMain;
		valueLabel.BackColor = Theme.Panel;
		valueLabel.TextAlign = ContentAlignment.MiddleRight;
		valueLabel.AutoEllipsis = true;

		Controls.Add(nameLabel, 0, 0);
		Controls.Add(valueLabel, 1, 0);
	}

	public void SetValue(string value)
	{
		ControlUpdate.SetText(valueLabel, value);
	}
}
