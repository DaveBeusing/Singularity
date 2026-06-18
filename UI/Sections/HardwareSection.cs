// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class HardwareSection : Panel
{
	public HardwareSection(HardwareInventory inventory)
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Panel;

		BuildUi(inventory);
	}

	private void BuildUi(HardwareInventory inventory)
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Motherboard,
			"HARDWARE");

		FlowLayoutPanel flow = CreateFlowPanel();

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

		Controls.Add(flow);

		Height =
			LayoutConstants.SectionHeaderHeight +
			CalculateFlowHeight(flow) +
			10;
	}

	private static FlowLayoutPanel CreateFlowPanel()
	{
		return new FlowLayoutPanel
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