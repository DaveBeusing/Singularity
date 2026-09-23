// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Persistence;
using Singularity.Core.Validation;

namespace Singularity.Tests.Application;

public sealed class QualificationProfileStoreTests
{
	[Fact]
	public async Task Store_RoundTripsCustomProfilesWithDistinctIdentity()
	{
		using TemporaryFile file = new();
		using QualificationProfileStore writer = new(file.Path);
		QualificationProfile first = CreateCustom("custom.alpha", "Lab");
		QualificationProfile second = CreateCustom("custom.beta", "Lab");

		await writer.SaveAsync([first, second], TestContext.Current.CancellationToken);

		using QualificationProfileStore reader = new(file.Path);
		await reader.LoadAsync(TestContext.Current.CancellationToken);

		Assert.Equal(QualificationProfileStoreState.Ready, reader.State);
		Assert.Equal(2, reader.Profiles.Count);
		Assert.Equal("Lab", reader.Profiles[0].Name);
		Assert.Equal("Lab", reader.Profiles[1].Name);
		Assert.NotEqual(reader.Profiles[0].Id, reader.Profiles[1].Id);
	}

	[Fact]
	public async Task Load_InvalidPersistedProfile_FailsClosed()
	{
		using TemporaryFile file = new();
		await File.WriteAllTextAsync(
			file.Path,
			"""
			{
			  "schemaVersion": 1,
			  "profiles": [
			    {
			      "id": "custom.invalid",
			      "name": "Invalid",
			      "recommendedDuration": "00:10:00",
			      "cpuMinimumLoadPercent": 50,
			      "cpuWarningLoadPercent": 75,
			      "memoryAllocationTolerancePercent": 90,
			      "memoryWarningTolerancePercent": 75,
			      "gpuMinimumLoadPercent": 85,
			      "gpuMaximumTemperatureCelsius": 85,
			      "gpuWarmupDuration": "00:00:10",
			      "gpuStabilityDuration": "00:00:03"
			    }
			  ]
			}
			""",
			TestContext.Current.CancellationToken);

		using QualificationProfileStore store = new(file.Path);
		await store.LoadAsync(TestContext.Current.CancellationToken);

		Assert.Equal(QualificationProfileStoreState.Failed, store.State);
		Assert.Empty(store.Profiles);
		Assert.NotNull(store.LastError);
	}

	[Fact]
	public async Task Catalog_ProtectsBuiltInsAndPersistsCustomLifecycle()
	{
		using TemporaryFile file = new();
		using QualificationProfileStore store = new(file.Path);
		QualificationProfileCatalog catalog = new(store);
		await catalog.LoadAsync(TestContext.Current.CancellationToken);

		Assert.False(await catalog.UpdateAsync(
			QualificationProfiles.Quick with { Name = "Changed" },
			TestContext.Current.CancellationToken));
		Assert.False(await catalog.DeleteAsync(
			QualificationProfiles.Quick.Id,
			TestContext.Current.CancellationToken));

		QualificationProfile? created = await catalog.CreateAsync(
			CreateCustom("pending", "Lab"),
			TestContext.Current.CancellationToken);
		Assert.NotNull(created);
		Assert.StartsWith("custom.", created!.Id, StringComparison.Ordinal);
		Assert.Contains(catalog.Profiles, profile => profile.Id == created.Id);

		QualificationProfile? duplicate = await catalog.DuplicateAsync(
			created,
			TestContext.Current.CancellationToken);
		Assert.NotNull(duplicate);
		Assert.Equal("Lab Copy", duplicate!.Name);
		Assert.NotEqual(created.Id, duplicate.Id);

		QualificationProfile? sameName = await catalog.CreateAsync(
			created with { Name = "Lab" },
			TestContext.Current.CancellationToken);
		Assert.NotNull(sameName);
		Assert.Equal(created.Name, sameName!.Name);
		Assert.NotEqual(created.Id, sameName.Id);

		Assert.True(await catalog.UpdateAsync(
			created with { CpuMinimumLoadPercent = 88 },
			TestContext.Current.CancellationToken));
		Assert.Equal(88, catalog.Profiles.Single(profile => profile.Id == created.Id).CpuMinimumLoadPercent);

		Assert.True(await catalog.DeleteAsync(
			created.Id,
			TestContext.Current.CancellationToken));
		Assert.DoesNotContain(catalog.Profiles, profile => profile.Id == created.Id);
		Assert.Contains(catalog.Profiles, profile => profile.Id == QualificationProfiles.Quick.Id);

		Assert.True(await catalog.ResetCustomAsync(TestContext.Current.CancellationToken));
		Assert.Equal(3, catalog.Profiles.Count);
		Assert.All(catalog.Profiles, profile => Assert.True(profile.IsBuiltIn));
	}

	private static QualificationProfile CreateCustom(string id, string name) =>
		QualificationProfiles.Standard with
		{
			Id = id,
			Name = name,
			Origin = QualificationProfileOrigin.Custom
		};

	private sealed class TemporaryFile : IDisposable
	{
		private readonly string directory =
			System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"SingularityTests-{Guid.NewGuid():N}");

		public TemporaryFile()
		{
			Directory.CreateDirectory(directory);
			Path = System.IO.Path.Combine(directory, "qualification-profiles.json");
		}

		public string Path { get; }

		public void Dispose()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}
}
