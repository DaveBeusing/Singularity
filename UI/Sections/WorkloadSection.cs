// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class WorkloadSection : Panel
{
	private const int CardHeight = LayoutConstants.CardHeight;

	public SingularityCheckBox CpuCheck { get; } = new();
	public SingularityCheckBox MemoryCheck { get; } = new();
	public SingularityCheckBox GpuCheck { get; } = new();

	public SingularityNumeric CpuThreadsInput { get; } = new();
	public SingularityNumeric MemoryGbInput { get; } = new();
	public SingularityNumeric GpuLoadInput { get; } = new();

	public WorkloadSection()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.WorkloadPanelWidth;
		Height = 365;
		BackColor = Theme.Panel;

		BuildUi();
	}

	private void BuildUi()
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Cpu,
			"WORKLOADS");

		Panel cpuCard = BuildCpuCard();
		Panel ramCard = BuildRamCard();
		Panel gpuCard = BuildGpuCard();

		Controls.AddRange([
			cpuCard,
			ramCard,
			gpuCard
		]);
	}

	private Panel BuildCpuCard()
	{
		Panel card = UiFactory.CreateCard(20, 60, 365, CardHeight);

		UiFactory.ConfigureCheckBox(CpuCheck, 22, 25, true);
		UiFactory.ConfigureNumeric(
			CpuThreadsInput,
			270,
			16,
			Environment.ProcessorCount,
			1,
			Environment.ProcessorCount * 4);

		card.Controls.AddRange([
			CpuCheck,
			UiFactory.CreateIcon(SingularityIconType.Cpu, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("CPU", 105, 23, 75),
			UiFactory.CreateMutedLabel("Threads", 190, 27, 75),
			CpuThreadsInput
		]);

		return card;
	}

	private Panel BuildRamCard()
	{
		Panel card = UiFactory.CreateCard(20, 150, 365, CardHeight);

		UiFactory.ConfigureCheckBox(MemoryCheck, 22, 25, true);
		UiFactory.ConfigureNumeric(
			MemoryGbInput,
			270,
			16,
			8,
			1,
			1024);

		card.Controls.AddRange([
			MemoryCheck,
			UiFactory.CreateIcon(SingularityIconType.Memory, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("RAM", 105, 23, 75),
			UiFactory.CreateMutedLabel("GB", 190, 27, 75),
			MemoryGbInput
		]);

		return card;
	}

	private Panel BuildGpuCard()
	{
		Panel card = UiFactory.CreateCard(20, 240, 365, CardHeight);

		UiFactory.ConfigureCheckBox(GpuCheck, 22, 25, false);
		UiFactory.ConfigureNumeric(
			GpuLoadInput,
			270,
			16,
			100,
			1,
			100);

		card.Controls.AddRange([
			GpuCheck,
			UiFactory.CreateIcon(SingularityIconType.Gpu, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("GPU", 105, 23, 75),
			UiFactory.CreateMutedLabel("Load %", 190, 27, 75),
			GpuLoadInput
		]);

		return card;
	}

}