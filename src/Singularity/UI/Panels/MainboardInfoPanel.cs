// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class MainboardInfoPanel : Panel
{
	public MainboardInfoPanel(MainboardInventory mainboard, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Controls.Add(new SingularityIcon
		{
			IconType = SingularityIconType.Motherboard,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 23,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		});

		Controls.Add(CreateLabel("BOARD", 64, 10, width - 84, Theme.TextMuted, 8.5F, true));
		Controls.Add(CreateLabel(mainboard.Name, 64, 30, width - 84, Theme.TextMain, 8.5F, false));
		Controls.Add(CreateLabel(mainboard.Details, 64, 50, width - 84, Theme.TextMuted, 8.5F, false));
	}

	private static Label CreateLabel(string text, int left, int top, int width, Color color, float size, bool bold)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 20,
			Font = new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular),
			ForeColor = color,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};
	}

}