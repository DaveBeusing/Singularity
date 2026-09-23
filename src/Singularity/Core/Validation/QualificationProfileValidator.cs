// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Validation;

public sealed record QualificationProfileValidationResult(
	IReadOnlyList<string> Errors)
{
	public bool IsValid => Errors.Count == 0;
}

public static class QualificationProfileValidator
{
	private const double MaximumTemperatureCelsius = 150;

	public static QualificationProfileValidationResult Validate(
		QualificationProfile profile)
	{
		ArgumentNullException.ThrowIfNull(profile);

		List<string> errors = [];

		if (string.IsNullOrWhiteSpace(profile.Id))
			errors.Add("Profile identity is required.");

		if (string.IsNullOrWhiteSpace(profile.Name))
			errors.Add("Profile name is required.");

		if (profile.RecommendedDuration <= TimeSpan.Zero)
			errors.Add("Recommended duration must be greater than zero.");

		ValidatePercentage(
			profile.CpuMinimumLoadPercent,
			"CPU minimum load",
			errors);
		ValidatePercentage(
			profile.CpuWarningLoadPercent,
			"CPU warning load",
			errors);
		ValidatePercentage(
			profile.MemoryAllocationTolerancePercent,
			"Memory allocation tolerance",
			errors);
		ValidatePercentage(
			profile.MemoryWarningTolerancePercent,
			"Memory warning tolerance",
			errors);
		ValidatePercentage(
			profile.GpuMinimumLoadPercent,
			"GPU minimum load",
			errors);

		if (profile.CpuWarningLoadPercent > profile.CpuMinimumLoadPercent)
			errors.Add("CPU warning load cannot exceed the CPU pass threshold.");

		if (profile.MemoryWarningTolerancePercent >
			profile.MemoryAllocationTolerancePercent)
		{
			errors.Add("Memory warning tolerance cannot exceed the memory pass tolerance.");
		}

		if (profile.GpuMaximumTemperatureCelsius <= 0 ||
			profile.GpuMaximumTemperatureCelsius > MaximumTemperatureCelsius)
		{
			errors.Add(
				$"GPU maximum temperature must be greater than 0 and no more than {MaximumTemperatureCelsius:0} °C.");
		}

		if (profile.GpuWarmupDuration < TimeSpan.Zero)
			errors.Add("GPU warm-up duration cannot be negative.");

		if (profile.GpuStabilityDuration < TimeSpan.Zero)
			errors.Add("GPU stability duration cannot be negative.");

		if (profile.RecommendedDuration > TimeSpan.Zero &&
			profile.GpuWarmupDuration + profile.GpuStabilityDuration >
			profile.RecommendedDuration)
		{
			errors.Add("GPU warm-up and stability time cannot exceed the recommended duration.");
		}

		return new QualificationProfileValidationResult(
			Array.AsReadOnly(errors.ToArray()));
	}

	public static void EnsureValid(QualificationProfile profile)
	{
		QualificationProfileValidationResult result = Validate(profile);
		if (result.IsValid)
			return;

		throw new ArgumentException(
			$"Invalid qualification profile: {string.Join(" ", result.Errors)}",
			nameof(profile));
	}

	private static void ValidatePercentage(
		double value,
		string name,
		ICollection<string> errors)
	{
		if (double.IsNaN(value) ||
			double.IsInfinity(value) ||
			value is < 0 or > 100)
		{
			errors.Add($"{name} must be between 0 and 100 percent.");
		}
	}
}
