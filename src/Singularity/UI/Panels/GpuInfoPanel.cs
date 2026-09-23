// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Panels;

public sealed class GpuInfoPanel : Panel
{
	public GpuInfoPanel(GpuInventory gpu, int width, int height)
	{
		Width = width;
		Height = height;
		BackColor = Theme.PanelLight;

		Controls.Add(new SingularityIcon
		{
			IconType = SingularityIconType.Gpu,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		});

		Controls.Add(CreateLabel(
			$"GPU · {Display(gpu.Vendor)}",
			64,
			8,
			250,
			Theme.TextMuted,
			8.5F,
			true,
			ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(gpu.Name, 64, 29, width - 260, Theme.TextMain, 8.8F, false, ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(
			$"{PcieLink(gpu.PcieGenerationCurrent, gpu.PcieWidthCurrent)} · D3D12 {(gpu.IsDirect3D12Capable ? "ready" : "unavailable")}",
			64,
			50,
			width - 260,
			Theme.TextMuted,
			8.2F,
			false,
			ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(
			$"Max {PcieLink(gpu.PcieGenerationMax, gpu.PcieWidthMax)}",
			64,
			68,
			width - 260,
			Theme.TextMuted,
			8.2F,
			false,
			ContentAlignment.MiddleLeft));
		Controls.Add(CreateLabel(Display(gpu.Vram), width - 185, 29, 165, Theme.TextMain, 8.8F, true, ContentAlignment.MiddleRight));
		Controls.Add(CreateLabel(Display(gpu.Temperature), width - 185, 50, 165, Theme.TextMain, 8.8F, true, ContentAlignment.MiddleRight));
	}

	private static string PcieLink(string? generation, string? width)
	{
		return IsAvailable(generation) && IsAvailable(width)
			? $"Gen{generation}x{width}"
			: "PCIe unavailable";
	}

	private static string Display(string? value) =>
		IsAvailable(value) ? value! : "Unavailable";

	private static bool IsAvailable(string? value)
	{
		return !string.IsNullOrWhiteSpace(value) &&
			!string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "Unavailable", StringComparison.OrdinalIgnoreCase);
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
			Font = bold ? ThemeFonts.CardTitle : ThemeFonts.CardText,
			ForeColor = color,
			BackColor = Theme.PanelLight,
			TextAlign = alignment,
			AutoEllipsis = true
		};
	}
}
