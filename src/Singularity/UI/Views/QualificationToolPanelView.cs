// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;

namespace Singularity.UI.Views;

public sealed class QualificationToolPanelView : Panel
{
	private readonly Label cpuValue = CreateMetricValue();
	private readonly Label memoryValue = CreateMetricValue();
	private readonly Label gpuValue = CreateMetricValue();
	private readonly Label progressValue = CreateMetricValue();

	public QualificationToolPanelView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.ToolPanel;

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = 1,
			BackColor = Theme.ToolPanel,
			Padding = new Padding(0)
		};

		for (int index = 0; index < 4; index++)
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

		layout.Controls.Add(CreateMetricCard("CPU", cpuValue), 0, 0);
		layout.Controls.Add(CreateMetricCard("SYSTEM MEMORY", memoryValue), 1, 0);
		layout.Controls.Add(CreateMetricCard("GPU", gpuValue), 2, 0);
		layout.Controls.Add(CreateMetricCard("QUALIFICATION", progressValue), 3, 0);

		Controls.Add(layout);
	}

	public void UpdateState(QualificationWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		cpuValue.Text = snapshot.CpuTelemetry;
		memoryValue.Text = snapshot.MemoryTelemetry;
		gpuValue.Text = snapshot.GpuTelemetry;
		progressValue.Text = snapshot.AutomatedState == QualificationRunState.Running
			? $"{snapshot.StepNumber}/{snapshot.StepCount} {snapshot.ActiveStep} • {snapshot.ProgressPercent:0}%"
			: snapshot.OverallState;

		gpuValue.ForeColor = snapshot.RequiredTelemetryUnavailable && snapshot.Configuration.EnableGpuWorkload
			? Theme.Warning
			: Theme.TextMain;
		memoryValue.ForeColor = snapshot.RequiredTelemetryUnavailable &&
			snapshot.Configuration.EnableMemoryWorkload &&
			string.Equals(snapshot.MemoryTelemetry, "Unavailable", StringComparison.Ordinal)
				? Theme.Warning
				: Theme.TextMain;
	}

	private static Panel CreateMetricCard(string title, Label value)
	{
		Panel panel = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.PanelLight,
			Margin = new Padding(0, 0, ThemeMetrics.Spacing, 0),
			Padding = new Padding(ThemeMetrics.Spacing)
		};

		Label titleLabel = new()
		{
			Dock = DockStyle.Top,
			Height = 24,
			Text = title,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		value.Dock = DockStyle.Fill;
		panel.Controls.Add(value);
		panel.Controls.Add(titleLabel);
		return panel;
	}

	private static Label CreateMetricValue()
	{
		return new Label
		{
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true
		};
	}
}
