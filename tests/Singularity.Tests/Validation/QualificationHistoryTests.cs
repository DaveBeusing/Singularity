// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Tests.Validation;

public sealed class QualificationHistoryTests
{
	[Fact]
	public void Add_KeepsTenNewestCompletedSessions()
	{
		QualificationHistory history = new();
		for (int index = 0; index < 12; index++)
		{
			QualificationSession session = new();
			session.Start(QualificationProfiles.Quick);
			session.Complete(index == 11 ? ValidationStatus.Pass : ValidationStatus.Warning);
			history.Add(session);
		}

		Assert.Equal(10, history.Records.Count);
		Assert.Equal(ValidationStatus.Pass, history.Records[0].Result);
		Assert.Equal(ValidationStatus.Warning, history.Records[^1].Result);
		Assert.True(history.Records[0].StartedAt >= history.Records[^1].StartedAt);
	}

	[Fact]
	public void Add_IgnoresSessionThatHasNotEnded()
	{
		QualificationHistory history = new();
		QualificationSession session = new();
		session.Start(QualificationProfiles.Standard);

		history.Add(session);

		Assert.Empty(history.Records);
	}
}
