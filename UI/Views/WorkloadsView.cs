// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Monitoring;
using Singularity.UI.Controls;
using Singularity.UI.Panels;

namespace Singularity.UI.Views;

public sealed class WorkloadsView : Panel
{
	private const int CardHeight = 78;

	private readonly SingularityCheckBox cpuCheck = new();
	private readonly SingularityCheckBox memoryCheck = new();
	private readonly SingularityCheckBox gpuCheck = new();

	private readonly SingularityNumeric cpuThreadsInput = new();
	private readonly SingularityNumeric memoryGbInput = new();
	private readonly SingularityNumeric gpuLoadInput = new();

	public Button StartButton { get; } = new();
	public Button StopButton { get; } = new();

	public MetricsPanel CpuMetricCard { get; private set; } = null!;
	public MetricsPanel AppRamMetricCard { get; private set; } = null!;
	public MetricsPanel SystemRamMetricCard { get; private set; } = null!;

	public WorkloadsView()
	{
		Left = 0;
		Top = 0;
		Width = 840;
		BackColor = Theme.Background;

		BuildUi();
	}

	public WorkloadOptions CreateOptions()
	{
		return new WorkloadOptions
		{
			EnableCpuWorkload = cpuCheck.Checked,
			EnableMemoryWorkload = memoryCheck.Checked,
			EnableGpuWorkload = gpuCheck.Checked,
			CpuThreads = cpuThreadsInput.Value,
			MemoryGb = memoryGbInput.Value,
			GpuLoadPercent = gpuLoadInput.Value
		};
	}

	public void UpdateMetrics(SystemSnapshot snapshot)
	{
		CpuMetricCard.ValueLabel.Text = $"{snapshot.ProcessCpuPercent:0.0} %";
		CpuMetricCard.Bar.Value = (int)Math.Clamp(snapshot.ProcessCpuPercent, 0, 100);

		AppRamMetricCard.ValueLabel.Text = $"{snapshot.ProcessMemoryMb} MB";
		AppRamMetricCard.Bar.Value = 0;

		SystemRamMetricCard.ValueLabel.Text =
			$"{snapshot.UsedPhysicalMemoryMb} / {snapshot.TotalPhysicalMemoryMb} MB";

		SystemRamMetricCard.Bar.Value =
			(int)Math.Clamp(snapshot.UsedPhysicalMemoryPercent, 0, 100);
	}

	private void BuildUi()
	{
		Panel workloadsPanel = UiFactory.CreatePanel(0, 0, 405, 365);
		UiFactory.AddSectionHeader(workloadsPanel, SingularityIconType.Cpu, "WORKLOADS");

		Panel cpuCard = BuildCpuCard();
		Panel ramCard = BuildRamCard();
		Panel gpuCard = BuildGpuCard();

		workloadsPanel.Controls.AddRange([
			cpuCard,
			ramCard,
			gpuCard
		]);

		Panel metricsPanel = UiFactory.CreatePanel(435, 0, 405, 275);
		UiFactory.AddSectionHeader(metricsPanel, SingularityIconType.Metrics, "MONITORING");

		CpuMetricCard = new MetricsPanel("APP CPU", Theme.Accent, 365, 80)
		{
			Left = 20,
			Top = 60
		};

		AppRamMetricCard = new MetricsPanel("APP RAM", Theme.Success, 365, 80)
		{
			Left = 20,
			Top = 145
		};

		SystemRamMetricCard = new MetricsPanel("SYSTEM RAM", Theme.Danger, 365, 80)
		{
			Left = 20,
			Top = 230
		};

		metricsPanel.Height = 330;

		metricsPanel.Controls.AddRange([
			CpuMetricCard,
			AppRamMetricCard,
			SystemRamMetricCard
		]);

		Panel controlPanel = UiFactory.CreatePanel(435, 350, 405, 150);
		UiFactory.AddSectionHeader(controlPanel, SingularityIconType.Play, "CONTROL");

		ConfigureButton(StartButton, "START", 20, 70, Theme.Success);
		ConfigureButton(StopButton, "STOP", 210, 70, Theme.Danger);

		controlPanel.Controls.AddRange([
			StartButton,
			StopButton
		]);

		Controls.AddRange([
			workloadsPanel,
			metricsPanel,
			controlPanel
		]);

		Height = controlPanel.Bottom;
	}

	private Panel BuildCpuCard()
	{
		Panel card = UiFactory.CreateCard(20, 60, 365, CardHeight);

		UiFactory.ConfigureCheckBox(cpuCheck, 22, 25, true);
		UiFactory.ConfigureNumeric(cpuThreadsInput, 270, 16, Environment.ProcessorCount, 1, Environment.ProcessorCount * 4);

		card.Controls.AddRange([
			cpuCheck,
			UiFactory.CreateIcon(SingularityIconType.Cpu, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("CPU", 105, 23, 75),
			UiFactory.CreateMutedLabel("Threads", 190, 27, 75),
			cpuThreadsInput
		]);

		return card;
	}

	private Panel BuildRamCard()
	{
		Panel card = UiFactory.CreateCard(20, 150, 365, CardHeight);

		UiFactory.ConfigureCheckBox(memoryCheck, 22, 25, true);
		UiFactory.ConfigureNumeric(memoryGbInput, 270, 16, 8, 1, 1024);

		card.Controls.AddRange([
			memoryCheck,
			UiFactory.CreateIcon(SingularityIconType.Memory, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("RAM", 105, 23, 75),
			UiFactory.CreateMutedLabel("GB", 190, 27, 75),
			memoryGbInput
		]);

		return card;
	}

	private Panel BuildGpuCard()
	{
		Panel card = UiFactory.CreateCard(20, 240, 365, CardHeight);

		UiFactory.ConfigureCheckBox(gpuCheck, 22, 25, false);
		UiFactory.ConfigureNumeric(gpuLoadInput, 270, 16, 100, 1, 100);

		card.Controls.AddRange([
			gpuCheck,
			UiFactory.CreateIcon(SingularityIconType.Gpu, 60, 24, Theme.TextMuted),
			UiFactory.CreateValueLabel("GPU", 105, 23, 75),
			UiFactory.CreateMutedLabel("Load %", 190, 27, 75),
			gpuLoadInput
		]);

		return card;
	}

	private static void ConfigureButton(Button button, string text, int left, int top, Color backColor)
	{
		button.Text = text;
		button.Left = left;
		button.Top = top;
		button.Width = 170;
		button.Height = 46;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.BackColor = backColor;
		button.ForeColor = Color.White;
		button.Font = ThemeFonts.Button;
	}

}