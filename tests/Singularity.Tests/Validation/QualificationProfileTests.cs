// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Tests.Validation;

public sealed class QualificationProfileTests
{
	[Fact]
	public void BuiltIns_RetainCanonicalValuesAndIdentity()
	{
		Assert.Equal("builtin.quick", QualificationProfiles.Quick.Id);
		Assert.Equal(TimeSpan.FromMinutes(5), QualificationProfiles.Quick.RecommendedDuration);
		Assert.Equal(75, QualificationProfiles.Quick.CpuMinimumLoadPercent);
		Assert.True(QualificationProfiles.Quick.IsBuiltIn);

		Assert.Equal("builtin.standard", QualificationProfiles.Standard.Id);
		Assert.Equal(TimeSpan.FromMinutes(15), QualificationProfiles.Standard.RecommendedDuration);
		Assert.Equal(80, QualificationProfiles.Standard.CpuMinimumLoadPercent);
		Assert.True(QualificationProfiles.Standard.IsBuiltIn);

		Assert.Equal("builtin.burnin", QualificationProfiles.BurnIn.Id);
		Assert.Equal(TimeSpan.FromHours(1), QualificationProfiles.BurnIn.RecommendedDuration);
		Assert.Equal(90, QualificationProfiles.BurnIn.CpuMinimumLoadPercent);
		Assert.True(QualificationProfiles.BurnIn.IsBuiltIn);
	}

	[Fact]
	public void Validate_AcceptsValidCustomProfile()
	{
		QualificationProfile profile = CreateCustom("custom.valid", "Lab");

		QualificationProfileValidationResult result = QualificationProfileValidator.Validate(profile);

		Assert.True(result.IsValid);
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void Validate_RejectsInvalidThresholdRelationshipsAndDuration()
	{
		QualificationProfile profile = CreateCustom("custom.invalid", "Invalid") with
		{
			RecommendedDuration = TimeSpan.Zero,
			CpuMinimumLoadPercent = 50,
			CpuWarningLoadPercent = 70,
			MemoryAllocationTolerancePercent = 60,
			MemoryWarningTolerancePercent = 80
		};

		QualificationProfileValidationResult result = QualificationProfileValidator.Validate(profile);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Contains("duration", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(result.Errors, error => error.Contains("CPU warning", StringComparison.Ordinal));
		Assert.Contains(result.Errors, error => error.Contains("Memory warning", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_RejectsOutOfRangeGpuTimingAndTemperature()
	{
		QualificationProfile profile = CreateCustom("custom.invalid-gpu", "Invalid GPU") with
		{
			RecommendedDuration = TimeSpan.FromSeconds(10),
			GpuMaximumTemperatureCelsius = 151,
			GpuWarmupDuration = TimeSpan.FromSeconds(8),
			GpuStabilityDuration = TimeSpan.FromSeconds(5)
		};

		QualificationProfileValidationResult result = QualificationProfileValidator.Validate(profile);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Contains("temperature", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(result.Errors, error => error.Contains("warm-up", StringComparison.OrdinalIgnoreCase));
	}

	internal static QualificationProfile CreateCustom(string id, string name) =>
		QualificationProfiles.Standard with
		{
			Id = id,
			Name = name,
			Origin = QualificationProfileOrigin.Custom
		};
}
