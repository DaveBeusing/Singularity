// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class StorageInfoPanel : Panel
{
	public StorageInfoPanel(StorageInventory storage, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Controls.Add(new SingularityIcon
		{
			IconType = SingularityIconType.Metrics,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		});

		Controls.Add(CreateLabel(
			"STORAGE",
			64,
			8,
			200,
			Theme.TextMuted,
			true,
			ContentAlignment.MiddleLeft));

		Controls.Add(CreateLabel(
			storage.Model,
			64,
			28,
			width - 280,
			Theme.TextMain,
			false,
			ContentAlignment.MiddleLeft));

		Controls.Add(CreateLabel(
			storage.Details,
			64,
			48,
			width - 280,
			Theme.TextMuted,
			false,
			ContentAlignment.MiddleLeft));

		Controls.Add(CreateLabel(
			$"FW {storage.FirmwareRevision}",
			64,
			66,
			width - 280,
			Theme.TextMuted,
			false,
			ContentAlignment.MiddleLeft));

		Controls.Add(CreateLabel(
			storage.Manufacturer,
			width - 220,
			28,
			200,
			Theme.TextMain,
			true,
			ContentAlignment.MiddleRight));

		Controls.Add(CreateLabel(
			storage.DeviceType,
			width - 220,
			48,
			200,
			Theme.TextMuted,
			false,
			ContentAlignment.MiddleRight));

		Controls.Add(CreateLabel(
			storage.SerialNumber,
			width - 220,
			66,
			200,
			Theme.TextMuted,
			false,
			ContentAlignment.MiddleRight));
	}

	private static Label CreateLabel(
		string text,
		int left,
		int top,
		int width,
		Color color,
		bool bold,
		ContentAlignment alignment)
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
			TextAlign = alignment,
			AutoEllipsis = true
		};
	}

}