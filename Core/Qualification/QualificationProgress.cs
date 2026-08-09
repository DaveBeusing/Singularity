// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Qualification;

public sealed record QualificationProgress(
	QualificationRunState State,
	string StepName,
	int StepNumber,
	int StepCount,
	TimeSpan StepElapsed,
	TimeSpan StepDuration)
{
	public double Percent => StepCount == 0 || StepDuration <= TimeSpan.Zero
		? 0
		: Math.Clamp(((StepNumber - 1) + StepElapsed.TotalMilliseconds / StepDuration.TotalMilliseconds) / StepCount * 100, 0, 100);
}
