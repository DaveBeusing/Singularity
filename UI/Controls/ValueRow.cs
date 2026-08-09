// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Layout;

namespace Singularity.UI.Controls;

public sealed class ValueRow : Panel
{
	private readonly Label valueLabel;

	public ValueRow(string title, string value)
	{
		Width = LayoutConstants.SectionContentWidth;
		Height = LayoutConstants.ValueRowHeight;
		BackColor = Theme.Panel;

		Label titleLabel = new()
		{
			Text = title,
			Left = 0,
			Top = 0,
			Width = LayoutConstants.ValueRowLabelWidth,
			Height = Height,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel
		};

		valueLabel = new Label
		{
			Text = value,
			Left = LayoutConstants.ValueRowValueLeft,
			Top = 0,
			Width = LayoutConstants.ValueRowValueWidth,
			Height = Height,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleRight
		};

		Controls.AddRange([titleLabel, valueLabel]);
	}

	public void SetValue(string value, Color? color = null)
	{
		ControlUpdate.SetText(valueLabel, value);
		if (color.HasValue)
			ControlUpdate.SetForeColor(valueLabel, color.Value);
	}
}
