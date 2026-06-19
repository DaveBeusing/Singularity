// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class ValidationSection : Panel
{
	private readonly ValidationItemControl cpuItem;
	private readonly ValidationItemControl memoryItem;
	private readonly ValidationItemControl overallItem;

	public ValidationSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 430;

		Width = LayoutConstants.MetricsPanelWidth;
		Height = 185;

		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"VALIDATION");

		cpuItem = new ValidationItemControl(
			"CPU",
			365,
			36)
		{
			Left = 20,
			Top = 55
		};

		memoryItem = new ValidationItemControl(
			"MEMORY",
			365,
			36)
		{
			Left = 20,
			Top = 98
		};

		overallItem = new ValidationItemControl(
			"OVERALL",
			365,
			36)
		{
			Left = 20,
			Top = 141
		};

		Controls.AddRange([
			cpuItem,
			memoryItem,
			overallItem
		]);

		Reset();
	}

	public void UpdateValidation(
		ValidationResult result)
	{
		cpuItem.UpdateStatus(
			result.CpuStatus,
			result.CpuMessage);

		memoryItem.UpdateStatus(
			result.MemoryStatus,
			result.MemoryMessage);

		ValidationSummary summary =
			new(result);

		overallItem.UpdateStatus(
			summary.OverallStatus,
			string.Empty);
	}

	public void Reset()
	{
		cpuItem.Reset();
		memoryItem.Reset();
		overallItem.Reset();
	}
}