// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public enum QualificationProfileOrigin
{
	BuiltIn,
	Custom
}

public sealed record QualificationProfile(
	string Name,
	TimeSpan RecommendedDuration,
	double CpuMinimumLoadPercent,
	double CpuWarningLoadPercent,
	double MemoryAllocationTolerancePercent,
	double MemoryWarningTolerancePercent,
	double GpuMinimumLoadPercent,
	double GpuMaximumTemperatureCelsius,
	TimeSpan GpuWarmupDuration,
	TimeSpan GpuStabilityDuration)
{
	public string Id { get; init; } =
		string.IsNullOrWhiteSpace(Name)
			? "custom.unspecified"
			: $"custom.{Name.Trim()}";

	public QualificationProfileOrigin Origin { get; init; } =
		QualificationProfileOrigin.Custom;

	public bool IsBuiltIn => Origin == QualificationProfileOrigin.BuiltIn;

	public QualificationProfile Snapshot() => this with { };
}

public static class QualificationProfiles
{
	public const string QuickId = "builtin.quick";
	public const string StandardId = "builtin.standard";
	public const string BurnInId = "builtin.burnin";

	public static QualificationProfile Quick { get; } = new(
		"Quick",
		TimeSpan.FromMinutes(5),
		75,
		45,
		85,
		70,
		75,
		90,
		TimeSpan.FromSeconds(5),
		TimeSpan.FromSeconds(2))
	{
		Id = QuickId,
		Origin = QualificationProfileOrigin.BuiltIn
	};

	public static QualificationProfile Standard { get; } = new(
		"Standard",
		TimeSpan.FromMinutes(15),
		80,
		50,
		90,
		75,
		85,
		85,
		TimeSpan.FromSeconds(10),
		TimeSpan.FromSeconds(3))
	{
		Id = StandardId,
		Origin = QualificationProfileOrigin.BuiltIn
	};

	public static QualificationProfile BurnIn { get; } = new(
		"BurnIn",
		TimeSpan.FromHours(1),
		90,
		70,
		95,
		85,
		90,
		80,
		TimeSpan.FromSeconds(20),
		TimeSpan.FromSeconds(5))
	{
		Id = BurnInId,
		Origin = QualificationProfileOrigin.BuiltIn
	};

	public static IReadOnlyList<QualificationProfile> All { get; } =
		Array.AsReadOnly([Quick, Standard, BurnIn]);

	public static bool IsBuiltInId(string? id) =>
		string.Equals(id, QuickId, StringComparison.Ordinal) ||
		string.Equals(id, StandardId, StringComparison.Ordinal) ||
		string.Equals(id, BurnInId, StringComparison.Ordinal);

	public static QualificationProfile? FindBuiltIn(string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;

		return All.FirstOrDefault(
			profile => string.Equals(profile.Id, id, StringComparison.Ordinal));
	}

	public static QualificationProfile? FindBuiltInByName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return null;

		return All.FirstOrDefault(
			profile => string.Equals(profile.Name, name, StringComparison.Ordinal));
	}
}
