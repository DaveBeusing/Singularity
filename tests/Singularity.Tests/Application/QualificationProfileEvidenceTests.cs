// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Persistence;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;

namespace Singularity.Tests.Application;

public sealed class QualificationProfileEvidenceTests
{
	[Fact]
	public async Task Archive_RoundTripPreservesEffectiveCustomProfile()
	{
		string directory = Path.Combine(Path.GetTempPath(), $"SingularityTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, "qualification-archive.json");
		try
		{
			QualificationProfile profile = QualificationProfiles.Standard with
			{
				Id = "custom.evidence",
				Name = "Evidence Profile",
				Origin = QualificationProfileOrigin.Custom,
				CpuMinimumLoadPercent = 87
			};
			QualificationRecord record = new()
			{
				StartedAt = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Local),
				FinishedAt = new DateTime(2026, 9, 23, 12, 10, 0, DateTimeKind.Local),
				Duration = TimeSpan.FromMinutes(10),
				Result = ValidationStatus.Pass,
				ExecutionMode = QualificationExecutionMode.Manual,
				ProfileName = profile.Name,
				Profile = profile
			};

			using (QualificationArchiveService writer = new(path))
			{
				await writer.SaveAsync(record, TestContext.Current.CancellationToken);
			}

			using QualificationArchiveService reader = new(path);
			await reader.LoadAsync(TestContext.Current.CancellationToken);

			QualificationRecord loaded = Assert.Single(reader.Records);
			Assert.Equal(profile.Id, loaded.Profile.Id);
			Assert.Equal(profile.Name, loaded.Profile.Name);
			Assert.Equal(87, loaded.Profile.CpuMinimumLoadPercent);
			Assert.Equal(QualificationProfileOrigin.Custom, loaded.Profile.Origin);
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}
}
