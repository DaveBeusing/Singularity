// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed record QualificationProfile(
	string Name,
	TimeSpan RecommendedDuration,
	double CpuMinimumLoadPercent,
	double CpuWarningLoadPercent,
	double MemoryAllocationTolerancePercent,
	double MemoryWarningTolerancePercent,
	double GpuMinimumLoadPercent,
	double GpuMaximumTemperatureCelsius);

public static class QualificationProfiles
{
	public static QualificationProfile Quick { get; } = new(
		"Quick",
		TimeSpan.FromMinutes(5),
		75,
		45,
		85,
		70,
		75,
		90);

	public static QualificationProfile Standard { get; } = new(
		"Standard",
		TimeSpan.FromMinutes(15),
		80,
		50,
		90,
		75,
		85,
		85);

	public static QualificationProfile BurnIn { get; } = new(
		"BurnIn",
		TimeSpan.FromHours(1),
		90,
		70,
		95,
		85,
		90,
		80);

	public static IReadOnlyList<QualificationProfile> All { get; } =
		[Quick, Standard, BurnIn];
}
