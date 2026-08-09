// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Core.Validation;

namespace Singularity.Tests.Reporting;

public sealed class QualificationReportGeneratorTests
{
	[Fact]
	public void Create_MapsFinalizedSessionAndValidation()
	{
		QualificationSession session = new();
		session.Start(QualificationProfiles.Standard);
		session.Complete(ValidationStatus.Warning);
		ValidationResult validation = new()
		{
			CpuStatus = ValidationStatus.Pass,
			MemoryStatus = ValidationStatus.Warning,
			GpuStatus = ValidationStatus.Unknown
		};

		QualificationReport report = new QualificationReportGenerator().Create(session, validation);

		Assert.Equal("Standard", report.Profile.Name);
		Assert.Equal(ValidationStatus.Pass, report.CpuResult);
		Assert.Equal(ValidationStatus.Warning, report.MemoryResult);
		Assert.Equal(ValidationStatus.Warning, report.OverallResult);
		Assert.Equal(session.StartTime, report.StartedAt);
		Assert.Equal(session.EndTime, report.FinishedAt);
	}

	[Fact]
	public void Create_RejectsSessionWithoutStartTime()
	{
		QualificationSession session = new();
		QualificationReportGenerator generator = new();

		Assert.Throws<InvalidOperationException>(() => generator.Create(session, new ValidationResult()));
	}
}
