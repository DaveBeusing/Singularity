// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Application.Persistence;

internal sealed class QualificationProfileDocument
{
	public const int CurrentSchemaVersion = 1;
	public int SchemaVersion { get; init; } = CurrentSchemaVersion;
	public IReadOnlyList<QualificationProfileDto> Profiles { get; init; } =
		Array.Empty<QualificationProfileDto>();
}

internal sealed class QualificationProfileDto
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public TimeSpan RecommendedDuration { get; init; }
	public double CpuMinimumLoadPercent { get; init; }
	public double CpuWarningLoadPercent { get; init; }
	public double MemoryAllocationTolerancePercent { get; init; }
	public double MemoryWarningTolerancePercent { get; init; }
	public double GpuMinimumLoadPercent { get; init; }
	public double GpuMaximumTemperatureCelsius { get; init; }
	public TimeSpan GpuWarmupDuration { get; init; }
	public TimeSpan GpuStabilityDuration { get; init; }

	public static QualificationProfileDto FromProfile(QualificationProfile profile) => new()
	{
		Id = profile.Id,
		Name = profile.Name,
		RecommendedDuration = profile.RecommendedDuration,
		CpuMinimumLoadPercent = profile.CpuMinimumLoadPercent,
		CpuWarningLoadPercent = profile.CpuWarningLoadPercent,
		MemoryAllocationTolerancePercent = profile.MemoryAllocationTolerancePercent,
		MemoryWarningTolerancePercent = profile.MemoryWarningTolerancePercent,
		GpuMinimumLoadPercent = profile.GpuMinimumLoadPercent,
		GpuMaximumTemperatureCelsius = profile.GpuMaximumTemperatureCelsius,
		GpuWarmupDuration = profile.GpuWarmupDuration,
		GpuStabilityDuration = profile.GpuStabilityDuration
	};

	public QualificationProfile ToProfile() => new(
		Name,
		RecommendedDuration,
		CpuMinimumLoadPercent,
		CpuWarningLoadPercent,
		MemoryAllocationTolerancePercent,
		MemoryWarningTolerancePercent,
		GpuMinimumLoadPercent,
		GpuMaximumTemperatureCelsius,
		GpuWarmupDuration,
		GpuStabilityDuration)
	{
		Id = Id,
		Origin = QualificationProfileOrigin.Custom
	};
}
