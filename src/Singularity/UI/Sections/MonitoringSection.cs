// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Monitoring.Models;
using Singularity.UI.Factories;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class MonitoringSection : Panel
{
	public MetricsPanel CpuMetricCard { get; private set; } = null!;
	public MetricsPanel GpuMetricCard { get; private set; } = null!;
	public MetricsPanel GpuMemoryMetricCard { get; private set; } = null!;
	public MetricsPanel MemoryMetricCard { get; private set; } = null!;

	public MonitoringSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 0;
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 415;
		BackColor = Theme.Panel;
		BuildUi();
	}

	public void UpdateMetrics(SystemSnapshot snapshot)
	{
		string cpuText;
		if (snapshot.CpuTemperatureAvailable)
		{
			cpuText = $"{snapshot.CpuLoadPercent:0.0} % | {snapshot.CpuTemperatureCelsius:0} °C";
		}
		else
		{
			cpuText = $"{snapshot.CpuLoadPercent:0.0} % | {snapshot.CpuTemperatureStatus}";
		}

		CpuMetricCard.UpdateMetric(
			cpuText,
			(int)Math.Clamp(snapshot.CpuLoadPercent, 0, 100));

		if (snapshot.GpuTelemetryAvailable)
		{
			GpuMetricCard.UpdateMetric(
				BuildGpuMainText(snapshot),
				(int)Math.Clamp(snapshot.GpuLoadPercent, 0, 100));
			GpuMemoryMetricCard.UpdateMetric(
				$"{snapshot.GpuMemoryUsedMb} / {snapshot.GpuMemoryTotalMb} MB",
				(int)Math.Clamp(snapshot.GpuMemoryUsedPercent, 0, 100));
		}
		else
		{
			GpuMetricCard.UpdateMetric(snapshot.GpuTelemetryStatus, 0);
			GpuMemoryMetricCard.UpdateMetric(snapshot.GpuTelemetryStatus, 0);
		}

		MemoryMetricCard.UpdateMetric(
			$"{snapshot.UsedPhysicalMemoryPercent:0.0} %",
			(int)Math.Clamp(snapshot.UsedPhysicalMemoryPercent, 0, 100));
	}

	private static string BuildGpuMainText(SystemSnapshot snapshot)
	{
		string text = $"{snapshot.GpuLoadPercent:0.0} % | {snapshot.GpuTemperatureCelsius} °C";

		if (snapshot.GpuPowerAvailable)
		{
			text += $" | {snapshot.GpuPowerWatts:0} W";
		}

		return text;
	}

	private void BuildUi()
	{
		UiFactory.AddSectionHeader(this, SingularityIconType.Metrics, "TELEMETRY");

		CpuMetricCard = new MetricsPanel("CPU LOAD", Theme.Accent, LayoutConstants.SectionContentWidth, 80)
		{
			Left = LayoutConstants.SectionPadding,
			Top = 60
		};

		GpuMetricCard = new MetricsPanel("GPU LOAD", Theme.Success, LayoutConstants.SectionContentWidth, 80)
		{
			Left = LayoutConstants.SectionPadding,
			Top = 145
		};

		GpuMemoryMetricCard = new MetricsPanel("GPU MEMORY", Theme.Success, LayoutConstants.SectionContentWidth, 80)
		{
			Left = LayoutConstants.SectionPadding,
			Top = 230
		};

		MemoryMetricCard = new MetricsPanel("SYSTEM MEMORY", Theme.Danger, LayoutConstants.SectionContentWidth, 80)
		{
			Left = LayoutConstants.SectionPadding,
			Top = 315
		};

		Controls.AddRange([
			CpuMetricCard,
			GpuMetricCard,
			GpuMemoryMetricCard,
			MemoryMetricCard
		]);
	}

}
