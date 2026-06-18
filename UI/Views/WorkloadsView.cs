// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Monitoring;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class WorkloadsView : Panel
{
	private WorkloadSection workloadSection = null!;
	private MonitoringSection monitoringSection = null!;
	private ControlSection controlSection = null!;

	public Button StartButton => controlSection.StartButton;
	public Button StopButton => controlSection.StopButton;

	public WorkloadsView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		BuildUi();
	}

	public WorkloadOptions CreateOptions()
	{
		return new WorkloadOptions
		{
			EnableCpuWorkload = workloadSection.CpuCheck.Checked,
			EnableMemoryWorkload = workloadSection.MemoryCheck.Checked,
			EnableGpuWorkload = workloadSection.GpuCheck.Checked,
			CpuThreads = workloadSection.CpuThreadsInput.Value,
			MemoryGb = workloadSection.MemoryGbInput.Value,
			GpuLoadPercent = workloadSection.GpuLoadInput.Value
		};
	}

	public void UpdateMetrics(SystemSnapshot snapshot)
	{
		monitoringSection.UpdateMetrics(snapshot);
	}

	private void BuildUi()
	{
		Controls.Clear();

		workloadSection = new WorkloadSection
		{
			Left = 0,
			Top = 0
		};

		monitoringSection = new MonitoringSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = 0
		};

		controlSection = new ControlSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = monitoringSection.Bottom + LayoutConstants.SectionGap
		};

		Controls.AddRange([
			workloadSection,
			monitoringSection,
			controlSection
		]);

		Height = Math.Max(
			workloadSection.Bottom,
			controlSection.Bottom);
	}

}