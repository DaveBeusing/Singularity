// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Core.Reporting;

public sealed class QualificationReportGenerator
{
	public QualificationReport Create(
		QualificationSession session,
		ValidationResult validationResult)
	{
		if (session.StartTime is null)
		{
			throw new InvalidOperationException("Session has no start time.");
		}

		if (session.EndTime is null)
		{
			throw new InvalidOperationException("Session has no end time.");
		}

		ValidationSummary summary = new(validationResult);

		return new QualificationReport
		{
			StartedAt = session.StartTime.Value,
			FinishedAt = session.EndTime.Value,
			Duration = session.Duration,
			CpuResult = validationResult.CpuStatus,
			MemoryResult = validationResult.MemoryStatus,
			OverallResult = summary.OverallStatus
		};
	}

}