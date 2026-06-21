// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Reporting;

public sealed class QualificationSummary
{
	public string StartedAt { get; init; } = string.Empty;
	public string FinishedAt { get; init; } = string.Empty;
	public string Duration { get; init; } = string.Empty;
	public string CpuResult { get; init; } = string.Empty;
	public string MemoryResult { get; init; } = string.Empty;
	public string OverallResult { get; init; } = string.Empty;

	public static QualificationSummary FromReport(QualificationReport report)
	{
		return new QualificationSummary
		{
			StartedAt = report.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
			FinishedAt = report.FinishedAt.ToString("yyyy-MM-dd HH:mm:ss"),
			Duration = report.Duration.ToString(@"hh\:mm\:ss"),
			CpuResult = report.CpuResult.ToString().ToUpperInvariant(),
			MemoryResult = report.MemoryResult.ToString().ToUpperInvariant(),
			OverallResult = report.OverallResult.ToString().ToUpperInvariant()
		};
	}

}