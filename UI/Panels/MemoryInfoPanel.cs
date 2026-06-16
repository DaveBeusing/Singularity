// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class MemoryInfoPanel : Panel
{
	public MemoryInfoPanel(MemoryInventory memory, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Controls.Add(new SingularityIcon
		{
			IconType = SingularityIconType.Memory,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		});

		Controls.Add(CreateLabel("MEMORY", 64, 8, 200, Theme.TextMuted, 8.5F, true, ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(memory.Slot, 64, 28, width - 280, Theme.TextMain, 8.8F, false, ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel($"{memory.Capacity} {memory.MemoryType} {memory.DimmType} {memory.EccType}", 64, 48, width - 280, Theme.TextMuted, 8.2F, false, ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(memory.Speed, 64, 66, width - 280, Theme.TextMuted, 8.2F, false, ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(memory.Manufacturer, width - 220, 28, 200, Theme.TextMain, 8.8F, true, ContentAlignment.MiddleRight));
		Controls.Add(CreateLabel(memory.PartNumber, width - 220, 48, 200, Theme.TextMuted, 8.2F, false, ContentAlignment.MiddleRight));
	}

	private static Label CreateLabel(string text, int left, int top, int width, Color color, float size, bool bold, ContentAlignment alignment)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 18,
			Font = new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular),
			ForeColor = color,
			BackColor = Theme.PanelLight,
			TextAlign = alignment,
			AutoEllipsis = true
		};
	}

}