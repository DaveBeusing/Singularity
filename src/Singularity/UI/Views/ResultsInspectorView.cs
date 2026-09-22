// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Reporting;

namespace Singularity.UI.Views;

public sealed class ResultsInspectorView : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label bodyLabel = new();

	public ResultsInspectorView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Inspector;
		Padding = new Padding(ThemeMetrics.Spacing);

		titleLabel.Dock = DockStyle.Top;
		titleLabel.Height = 34;
		titleLabel.Font = ThemeFonts.Header;
		titleLabel.ForeColor = Theme.TextMain;
		titleLabel.BackColor = Theme.Inspector;

		bodyLabel.Dock = DockStyle.Fill;
		bodyLabel.Font = ThemeFonts.CardText;
		bodyLabel.ForeColor = Theme.TextMuted;
		bodyLabel.BackColor = Theme.Inspector;
		bodyLabel.TextAlign = ContentAlignment.TopLeft;

		Controls.Add(bodyLabel);
		Controls.Add(titleLabel);
		UpdateState(null, ResultsWorkspaceSnapshot.Empty);
	}

	public void UpdateState(string? contextId, ResultsWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		string context = contextId ?? "latest";
		titleLabel.Text = context switch
		{
			"validation" => "VALIDATION",
			"statistics" => "STATISTICS",
			_ => "LATEST RESULT"
		};

		if (!snapshot.HasResult)
		{
			bodyLabel.Text = "No completed qualification result is available.";
			return;
		}

		bodyLabel.Text = context switch
		{
			"validation" =>
				$"Overall  {StatusStyle.Format(snapshot.OverallStatus)}\r\n\r\n" +
				$"CPU      {StatusStyle.Format(snapshot.CpuStatus)}\r\n" +
				$"Memory   {StatusStyle.Format(snapshot.MemoryStatus)}\r\n" +
				$"GPU      {StatusStyle.Format(snapshot.GpuStatus)}",
			"statistics" => BuildStatistics(snapshot.TelemetryStatistics),
			_ =>
				$"Profile\r\n{snapshot.ProfileName}\r\n\r\n" +
				$"Mode\r\n{snapshot.ExecutionMode}\r\n\r\n" +
				$"Started\r\n{snapshot.StartedAt:G}\r\n\r\n" +
				$"Duration\r\n{snapshot.Duration:hh\\:mm\\:ss}"
		};
	}

	private static string BuildStatistics(SessionTelemetryStatistics statistics)
	{
		return string.Join(
			"\r\n\r\n",
			[
				FormatMetric("CPU load", statistics.CpuLoadPercent, "%"),
				FormatMetric("Memory", statistics.SystemMemoryUsagePercent, "%"),
				FormatMetric("GPU load", statistics.GpuLoadPercent, "%"),
				FormatMetric("GPU temperature", statistics.GpuTemperatureCelsius, "°C"),
				FormatMetric("GPU power", statistics.GpuPowerWatts, "W"),
				FormatMetric("VRAM", statistics.GpuVramUsagePercent, "%")
			]);
	}

	private static string FormatMetric(string name, MetricStatistics? statistics, string unit)
	{
		return statistics is null
			? $"{name}\r\nUnavailable"
			: $"{name}\r\nAvg {statistics.Average:0.0} {unit} • Max {statistics.Maximum:0.0} {unit}";
	}
}
