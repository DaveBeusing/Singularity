// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class CpuInfoPanel : Panel
{
	public CpuInfoPanel(CpuInventory cpu, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		SingularityIcon icon = new()
		{
			IconType = SingularityIconType.Cpu,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};

		Controls.Add(icon);
		Controls.Add(CreateLabel("CPU", 64, 8, width - 84, Theme.TextMuted, 8.5F, true));
		Controls.Add(CreateLabel(cpu.Name, 64, 28, width - 84, Theme.TextMain, 8.8F, false));
		Controls.Add(CreateLabel(cpu.CoreThreadInfo, 64, 48, width - 84, Theme.TextMuted, 8.2F, false));
		Controls.Add(CreateLabel(cpu.CacheInfo, 64, 66, width - 84, Theme.TextMuted, 8.2F, false));
		Controls.Add(CreateLabel(cpu.PlatformInfo, 64, 84, width - 84, Theme.TextMuted, 8.2F, false));
	}

	private static Label CreateLabel(string text, int left, int top, int width, Color color, float size, bool bold)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 18,
			Font = bold ? ThemeFonts.CardTitle : ThemeFonts.CardText,
			ForeColor = color,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};
	}

}