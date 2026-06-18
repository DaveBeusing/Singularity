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

		BuildUi();
	}

	private void BuildUi()
	{
		Controls.Clear();

		HardwareInventory inventory = hardwareProvider.Read();

		Panel osPanel = CreateOsPanel(inventory.Os);

		Panel hardwarePanel = CreateHardwarePanel(inventory);
		hardwarePanel.Top = osPanel.Bottom + LayoutConstants.SectionGap;

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
			LayoutConstants.CardHeight)
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
			Height = 1,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			BackColor = Theme.Panel,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};

		AddHardwareCard(
			flow,
			new MainboardInfoPanel(
				inventory.Mainboard,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.CardHeight));

		AddHardwareCard(
			flow,
			new CpuInfoPanel(
				inventory.Cpu,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.LargeCardHeight));

		AddHardwareCard(
			flow,
			new GpuInfoPanel(
				inventory.Gpu,
				LayoutConstants.HardwareCardWidth,
				LayoutConstants.LargeCardHeight));

		foreach (MemoryInventory module in inventory.MemoryModules)
		{
			AddHardwareCard(
				flow,
				new MemoryInfoPanel(
					module,
					LayoutConstants.HardwareCardWidth,
					LayoutConstants.LargeCardHeight));
		}

		panel.Controls.Add(flow);

		int flowHeight = CalculateFlowHeight(flow);

		panel.Height =
			LayoutConstants.SectionHeaderHeight +
			flowHeight +
			10;

		return panel;
	}

	private static void AddHardwareCard(FlowLayoutPanel flow, Control card)
	{
		card.Margin = new Padding(0, 0, 0, LayoutConstants.CardGap);
		flow.Controls.Add(card);
	}

	private static int CalculateFlowHeight(FlowLayoutPanel flow)
	{
		int height = 0;

		foreach (Control control in flow.Controls)
		{
			height += control.Height;
			height += control.Margin.Top;
			height += control.Margin.Bottom;
		}

		return height;
	}
}