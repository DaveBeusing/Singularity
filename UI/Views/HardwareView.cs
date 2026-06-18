// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;
using Singularity.UI.Controls;
using Singularity.UI.Panels;
using Singularity.UI.Cards;

namespace Singularity.UI.Views;

public sealed class HardwareView : Panel
{
	private const int MainWidth = 840;
	private const int HardwareCardWidth = 800;
	private const int HardwareCardLeft = 20;
	private const int HardwareCardGap = 10;
	private const int SectionHeaderHeight = 55;
	private const int StandardHardwareCardHeight = 78;
	private const int LargeHardwareCardHeight = 110;

	private readonly HardwareProvider hardwareProvider = new();

	public HardwareView()
	{
		Left = 0;
		Top = 0;
		Width = MainWidth;
		BackColor = Theme.Background;

		BuildUi();
	}

	private void BuildUi()
	{
		Controls.Clear();

		HardwareInventory inventory = hardwareProvider.Read();

		int contentTop = 0;

		Panel osPanel = UiFactory.CreatePanel(0, contentTop, MainWidth, 150);
		UiFactory.AddSectionHeader(osPanel, SingularityIconType.Metrics, "OS");

		OsInfoPanel osInfoPanel = new(inventory.Os, 800, 78)
		{
			Left = 20,
			Top = 55
		};

		osPanel.Controls.Add(osInfoPanel);
		contentTop = osPanel.Bottom + 20;

		Panel hardwarePanel = BuildHardwarePanel(inventory, contentTop);

		Controls.AddRange([
			osPanel,
			hardwarePanel
		]);

		Height = hardwarePanel.Bottom;
	}

	private static Panel BuildHardwarePanel(HardwareInventory inventory, int top)
	{
		int memoryCount = inventory.MemoryModules.Count;

		int hardwarePanelHeight =
			SectionHeaderHeight +
			StandardHardwareCardHeight + HardwareCardGap +
			LargeHardwareCardHeight + HardwareCardGap +
			LargeHardwareCardHeight + HardwareCardGap +
			memoryCount * (LargeHardwareCardHeight + HardwareCardGap) +
			10;

		Panel hardwarePanel = UiFactory.CreatePanel(0, top, MainWidth, hardwarePanelHeight);
		UiFactory.AddSectionHeader(hardwarePanel, SingularityIconType.Motherboard, "HARDWARE");

		int hardwareTop = SectionHeaderHeight;

		MainboardInfoPanel mainboardInfoPanel = new(inventory.Mainboard, HardwareCardWidth, StandardHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};

		hardwareTop += StandardHardwareCardHeight + HardwareCardGap;

		CpuInfoPanel cpuInfoPanel = new(inventory.Cpu, HardwareCardWidth, LargeHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};

		hardwareTop += LargeHardwareCardHeight + HardwareCardGap;

		GpuInfoPanel gpuInfoPanel = new(inventory.Gpu, HardwareCardWidth, LargeHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};

		hardwareTop += LargeHardwareCardHeight + HardwareCardGap;

		hardwarePanel.Controls.AddRange([
			mainboardInfoPanel,
			cpuInfoPanel,
			gpuInfoPanel
		]);

		foreach (MemoryInventory module in inventory.MemoryModules)
		{
			MemoryInfoPanel memoryInfoPanel = new(module, HardwareCardWidth, LargeHardwareCardHeight)
			{
				Left = HardwareCardLeft,
				Top = hardwareTop
			};

			hardwarePanel.Controls.Add(memoryInfoPanel);
			hardwareTop += LargeHardwareCardHeight + HardwareCardGap;
		}

		hardwarePanel.Height = hardwareTop + 10;

		return hardwarePanel;
	}

}