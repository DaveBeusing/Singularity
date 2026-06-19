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
	public MetricsPanel GpuMetricCard { get; private set; } = null!;
	public MetricsPanel MemoryMetricCard { get; private set; } = null!;

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
		CpuMetricCard.ValueLabel.Text = $"{snapshot.CpuLoadPercent:0.0} %";
		CpuMetricCard.Bar.Value = (int)Math.Clamp(snapshot.CpuLoadPercent, 0, 100);

		if (snapshot.GpuTelemetryAvailable)
		{
			GpuMetricCard.ValueLabel.Text =
				$"{snapshot.GpuLoadPercent:0.0} % | {snapshot.GpuTemperatureCelsius} °C";

			GpuMetricCard.Bar.Value =
				(int)Math.Clamp(snapshot.GpuLoadPercent, 0, 100);
		}
		else
		{
			GpuMetricCard.ValueLabel.Text = snapshot.GpuTelemetryStatus;
			GpuMetricCard.Bar.Value = 0;
		}

		MemoryMetricCard.ValueLabel.Text =
			$"{snapshot.UsedPhysicalMemoryPercent:0.0} %";

		MemoryMetricCard.Bar.Value =
			(int)Math.Clamp(snapshot.UsedPhysicalMemoryPercent, 0, 100);
	}

	private void BuildUi()
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"TELEMETRY");

		CpuMetricCard = new MetricsPanel("CPU LOAD", Theme.Accent, 365, 80)
		{
			Left = 20,
			Top = 60
		};

		GpuMetricCard = new MetricsPanel("GPU LOAD", Theme.Success, 365, 80)
		{
			Left = 20,
			Top = 145
		};

		MemoryMetricCard = new MetricsPanel("MEMORY", Theme.Danger, 365, 80)
		{
			Left = 20,
			Top = 230
		};

		Controls.AddRange([
			CpuMetricCard,
			GpuMetricCard,
			MemoryMetricCard
		]);
	}

}