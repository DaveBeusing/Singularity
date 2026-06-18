// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;

namespace Singularity.UI.Views;

public sealed class HardwareView : Panel
{
	private readonly HardwareProvider hardwareProvider = new();

	public HardwareView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;
		AutoScroll = true;

		BuildUi();
	}

	private void BuildUi()
	{
		Controls.Clear();

		HardwareInventory inventory = hardwareProvider.Read();

		int contentTop = 0;

		Panel osPanel = CreateOsPanel(inventory.Os);

		contentTop = osPanel.Bottom + LayoutConstants.SectionGap;

		Panel hardwarePanel = CreateHardwarePanel(inventory);

		hardwarePanel.Top = contentTop;

		Controls.Add(osPanel);
		Controls.Add(hardwarePanel);

		Height = hardwarePanel.Bottom;
	}

	private static Panel CreateOsPanel(OsInventory osInventory)
	{
		Panel panel = UiFactory.CreatePanel(
			0,
			0,
			LayoutConstants.MainWidth,
			150);

		UiFactory.AddSectionHeader(
			panel,
			SingularityIconType.Metrics,
			"OS");

		OsInfoPanel osInfoPanel = new(
			osInventory,
			LayoutConstants.HardwareCardWidth,
			78)
		{
			Left = LayoutConstants.HardwareCardLeft,
			Top = LayoutConstants.SectionHeaderHeight
		};

		panel.Controls.Add(osInfoPanel);

		return panel;
	}

	private static Panel CreateHardwarePanel(HardwareInventory inventory)
	{
		Panel panel = UiFactory.CreatePanel(
			0,
			0,
			LayoutConstants.MainWidth,
			200);

		UiFactory.AddSectionHeader(
			panel,
			SingularityIconType.Motherboard,
			"HARDWARE");

		FlowLayoutPanel flow = new()
		{
			Left = LayoutConstants.HardwareCardLeft,
			Top = LayoutConstants.SectionHeaderHeight,
			Width = LayoutConstants.HardwareCardWidth,
			AutoSize = true,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			BackColor = Theme.Panel,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};

		flow.Controls.Add(
			new MainboardInfoPanel(
				inventory.Mainboard,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.CardHeight));

		flow.Controls.Add(
			new CpuInfoPanel(
				inventory.Cpu,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.LargeCardHeight));

		flow.Controls.Add(
			new GpuInfoPanel(
				inventory.Gpu,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.LargeCardHeight));

		foreach (MemoryInventory module in inventory.MemoryModules)
		{
			flow.Controls.Add(
				new MemoryInfoPanel(
					module,
					LayoutConstants.HardwareCardWidth,
					LayoutConstants.LargeCardHeight));
		}

		panel.Controls.Add(flow);

		panel.Height =
			LayoutConstants.SectionHeaderHeight +
			flow.PreferredSize.Height +
			10;

		return panel;
	}

}