// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.UI.Cards;

public sealed class OsInfoPanel : Panel
{
	public OsInfoPanel(OsInventory os, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Controls.Add(CreateLabel(os.Name, 18, 10, width - 36, Theme.TextMain, 9.5F));
		Controls.Add(CreateLabel($"Version {os.Version} | {os.Architecture} | Build {os.Build}", 18, 32, width - 36, Theme.TextMuted, 8.5F));
		Controls.Add(CreateLabel($"Installed {os.InstallDate} | Boot {os.BootTime}", 18, 52, width - 36, Theme.TextMuted, 8.5F));
	}

	private static Label CreateLabel(string text, int left, int top, int width, Color color, float size)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 20,
			Font = new Font("Consolas", size),
			ForeColor = color,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};
	}

}