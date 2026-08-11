// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Controls;
using Singularity.UI.Factories;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class ValidationSection : Panel
{
	private readonly StatusRow cpuItem;
	private readonly StatusRow memoryItem;
	private readonly StatusRow gpuItem;
	private readonly StatusRow overallItem;

	public ValidationSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 430;

		Width = LayoutConstants.MetricsPanelWidth;
		Height = 228;

		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"VALIDATION");

		cpuItem = new StatusRow("CPU")
		{
			Left = LayoutConstants.SectionPadding,
			Top = 55
		};

		memoryItem = new StatusRow("MEMORY")
		{
			Left = LayoutConstants.SectionPadding,
			Top = 55 + LayoutConstants.StatusRowHeight + LayoutConstants.StatusRowGap
		};

		gpuItem = new StatusRow("GPU")
		{
			Left = LayoutConstants.SectionPadding,
			Top = 55 + (LayoutConstants.StatusRowHeight + LayoutConstants.StatusRowGap) * 2
		};

		overallItem = new StatusRow("OVERALL")
		{
			Left = LayoutConstants.SectionPadding,
			Top = 55 + (LayoutConstants.StatusRowHeight + LayoutConstants.StatusRowGap) * 3
		};

		Controls.AddRange([
			cpuItem,
			memoryItem,
			gpuItem,
			overallItem
		]);

		Reset();
	}

	public void UpdateValidation(
		ValidationResult result)
	{
		cpuItem.SetStatus(result.CpuStatus);
		memoryItem.SetStatus(result.MemoryStatus);
		gpuItem.SetStatus(result.GpuStatus);

		ValidationSummary summary =
			new(result);

		overallItem.SetStatus(summary.OverallStatus);
	}

	public void Reset()
	{
		cpuItem.Reset();
		memoryItem.Reset();
		gpuItem.Reset();
		overallItem.Reset();
	}
}
