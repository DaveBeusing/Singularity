// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Monitoring;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class MonitoringSection : Panel
{
	public MetricsPanel CpuMetricCard { get; private set; } = null!;
	public MetricsPanel AppRamMetricCard { get; private set; } = null!;
	public MetricsPanel SystemRamMetricCard { get; private set; } = null!;

	public MonitoringSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 0;
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 330;
		BackColor = Theme.Panel;

		BuildUi();
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
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"MONITORING");

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

		Controls.AddRange([
			CpuMetricCard,
			AppRamMetricCard,
			SystemRamMetricCard
		]);
	}

}