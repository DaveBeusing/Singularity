// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;

namespace Singularity.UI.Views;

internal static class UiFactory
{
	public static Panel CreatePanel(int left, int top, int width, int height)
	{
		return new Panel
		{
			Left = left,
			Top = top,
			Width = width,
			Height = height,
			BackColor = Theme.Panel
		};
	}

	public static Panel CreateCard(int left, int top, int width, int height)
	{
		return new Panel
		{
			Left = left,
			Top = top,
			Width = width,
			Height = height,
			BackColor = Theme.PanelLight
		};
	}

	public static Label CreateSectionTitle(string text, int left, int top)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = 250,
			Height = 25,
			Font = new Font("Segoe UI", 10, FontStyle.Bold),
			ForeColor = Theme.Accent,
			BackColor = Theme.Panel
		};
	}

	public static void AddSectionHeader(Panel parent, SingularityIconType iconType, string title)
	{
		SingularityIcon icon = new()
		{
			IconType = iconType,
			IconColor = Theme.Accent,
			Left = 20,
			Top = 14,
			Width = 32,
			Height = 32,
			BackColor = Theme.Panel
		};

		Label label = CreateSectionTitle(title, 60, 14);

		parent.Controls.AddRange([
			icon,
			label
		]);
	}

	public static Label CreateMutedLabel(string text, int left, int top, int width)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 22,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};
	}

	public static Label CreateValueLabel(string text, int left, int top, int width)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 24,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight
		};
	}

	public static SingularityIcon CreateIcon(SingularityIconType type, int left, int top, Color color)
	{
		return new SingularityIcon
		{
			IconType = type,
			IconColor = color,
			Left = left,
			Top = top,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};
	}

	public static void ConfigureCheckBox(SingularityCheckBox checkBox, int left, int top, bool isChecked)
	{
		checkBox.Left = left;
		checkBox.Top = top;
		checkBox.Width = 28;
		checkBox.Height = 28;
		checkBox.Checked = isChecked;
		checkBox.BackColor = Theme.PanelLight;
	}

	public static void ConfigureNumeric(SingularityNumeric numeric, int left, int top, int value, int minimum, int maximum)
	{
		numeric.Left = left;
		numeric.Top = top;
		numeric.Width = 82;
		numeric.Height = 46;
		numeric.Minimum = minimum;
		numeric.Maximum = maximum;
		numeric.Value = value;
		numeric.BackColor = Theme.PanelLight;
	}

}