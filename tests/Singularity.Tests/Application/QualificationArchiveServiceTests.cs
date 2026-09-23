// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Persistence;
using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;

namespace Singularity.Tests.Application;

public sealed class QualificationArchiveServiceTests
{
	[Fact]
	public async Task LoadAsync_WhenArchiveDoesNotExist_IsReadyAndEmpty()
	{
		using TemporaryDirectory temporary = new();
		using QualificationArchiveService archive = new(temporary.ArchivePath);

		await archive.LoadAsync();

		Assert.Equal(QualificationArchiveState.Ready, archive.State);
		Assert.Empty(archive.Records);
		Assert.Null(archive.LastError);
	}

	[Fact]
	public async Task SaveAsync_ReloadsFrozenReportAndMultiGpuEvidence()
	{
		using TemporaryDirectory temporary = new();
		QualificationRecord expected = CreateRecord(0, includeReport: true, gpuCount: 2);

		using (QualificationArchiveService archive = new(temporary.ArchivePath))
		{
			await archive.LoadAsync();
			await archive.SaveAsync(expected);

			Assert.Equal(QualificationArchiveState.Ready, archive.State);
			Assert.True(File.Exists(temporary.ArchivePath));
		}

		using QualificationArchiveService reloaded = new(temporary.ArchivePath);
		await reloaded.LoadAsync();

		QualificationRecord actual = Assert.Single(reloaded.Records);
		Assert.Equal(expected.StartedAt, actual.StartedAt);
		Assert.Equal(expected.FinishedAt, actual.FinishedAt);
		Assert.Equal(expected.Result, actual.Result);
		Assert.Equal(expected.ExecutionMode, actual.ExecutionMode);
		Assert.Equal(expected.ProfileName, actual.ProfileName);
		Assert.NotNull(actual.Report);
		Assert.Equal(ValidationStatus.Pass, actual.Report!.CpuResult);
		Assert.Equal(2, actual.GpuEvidence.Count);
		Assert.Equal(["GPU-0", "GPU-1"], actual.GpuEvidence.Select(item => item.Identifier));
		Assert.Equal(2, actual.TelemetryStatistics.Gpus.Count);
	}

	[Fact]
	public async Task SaveAsync_OrdersNewestFirstAndHonorsRetention()
	{
		using TemporaryDirectory temporary = new();
		using QualificationArchiveService archive = new(temporary.ArchivePath, retentionLimit: 2);
		await archive.LoadAsync();

		await archive.SaveAsync(CreateRecord(0));
		await archive.SaveAsync(CreateRecord(2));
		await archive.SaveAsync(CreateRecord(1));

		Assert.Equal(2, archive.Records.Count);
		Assert.Equal(CreateTimestamp(2).AddMinutes(1), archive.Records[0].FinishedAt);
		Assert.Equal(CreateTimestamp(1).AddMinutes(1), archive.Records[1].FinishedAt);
	}

	[Fact]
	public async Task LoadAsync_WhenArchiveIsCorrupted_FailsWithoutThrowing()
	{
		using TemporaryDirectory temporary = new();
		await File.WriteAllTextAsync(temporary.ArchivePath, "{ this is not valid json");

		using QualificationArchiveService archive = new(temporary.ArchivePath);
		await archive.LoadAsync();

		Assert.Equal(QualificationArchiveState.Failed, archive.State);
		Assert.Empty(archive.Records);
		Assert.NotNull(archive.LastError);
	}

	[Fact]
	public async Task LoadAsync_WhenSchemaVersionIsUnsupported_FailsClosed()
	{
		using TemporaryDirectory temporary = new();
		await File.WriteAllTextAsync(
			temporary.ArchivePath,
			"{\"schemaVersion\":999,\"records\":[]}");

		using QualificationArchiveService archive = new(temporary.ArchivePath);
		await archive.LoadAsync();

		Assert.Equal(QualificationArchiveState.Failed, archive.State);
		Assert.Empty(archive.Records);
		Assert.Contains("Unsupported", archive.LastError);
	}

	[Fact]
	public async Task SaveAsync_WhenExistingArchiveIsLocked_PreservesPriorValidArchive()
	{
		using TemporaryDirectory temporary = new();
		QualificationRecord original = CreateRecord(0);
		QualificationRecord replacement = CreateRecord(1);

		using (QualificationArchiveService archive = new(temporary.ArchivePath))
		{
			await archive.LoadAsync();
			await archive.SaveAsync(original);

			using (new FileStream(
				temporary.ArchivePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.None))
			{
				await archive.SaveAsync(replacement);
				Assert.Equal(QualificationArchiveState.Failed, archive.State);
			}
		}

		using QualificationArchiveService reloaded = new(temporary.ArchivePath);
		await reloaded.LoadAsync();

		QualificationRecord persisted = Assert.Single(reloaded.Records);
		Assert.Equal(original.StartedAt, persisted.StartedAt);
		Assert.Equal(original.FinishedAt, persisted.FinishedAt);
	}

	[Fact]
	public async Task SaveAsync_WhenStorageDirectoryCannotBeCreated_ReportsFailure()
	{
		using TemporaryDirectory temporary = new();
		string blockedParent = Path.Combine(temporary.Path, "blocked");
		await File.WriteAllTextAsync(blockedParent, "not a directory");
		string archivePath = Path.Combine(blockedParent, QualificationArchiveService.ArchiveFileName);

		using QualificationArchiveService archive = new(archivePath);
		await archive.SaveAsync(CreateRecord(0));

		Assert.Equal(QualificationArchiveState.Failed, archive.State);
		Assert.NotNull(archive.LastError);
		Assert.Empty(archive.Records);
	}

	[Fact]
	public async Task ClearAsync_DeletesStoredEvidence()
	{
		using TemporaryDirectory temporary = new();
		using QualificationArchiveService archive = new(temporary.ArchivePath);
		await archive.LoadAsync();
		await archive.SaveAsync(CreateRecord(0));

		await archive.ClearAsync();

		Assert.Equal(QualificationArchiveState.Ready, archive.State);
		Assert.Empty(archive.Records);
		Assert.False(File.Exists(temporary.ArchivePath));
	}

	[Fact]
	public async Task CoordinatorLoad_RehydratesResultsAndReportsFromArchive()
	{
		using TemporaryDirectory temporary = new();
		using (QualificationArchiveService writer = new(temporary.ArchivePath))
		{
			await writer.LoadAsync();
			await writer.SaveAsync(CreateRecord(0, includeReport: true, gpuCount: 1));
		}

		using QualificationArchiveService reader = new(temporary.ArchivePath);
		QualificationCoordinator coordinator = new(new IdleWorkloadController(), reader);

		await coordinator.LoadArchiveAsync();

		QualificationRecord record = Assert.Single(coordinator.EvidenceRecords);
		ResultsWorkspaceSnapshot results = ResultsWorkspaceState.Create(coordinator.EvidenceRecords);
		ReportsWorkspaceSnapshot reports =
			new ReportsWorkspaceState().CreateSnapshot(coordinator.EvidenceRecords, inventoryAvailable: true);

		Assert.Equal(QualificationArchiveState.Ready, coordinator.ArchiveState);
		Assert.True(results.HasResult);
		Assert.Equal(record.Result, results.OverallStatus);
		Assert.Same(record, reports.SelectedRecord);
		Assert.True(reports.CanExport);
	}

	private static QualificationRecord CreateRecord(
		int minuteOffset,
		bool includeReport = true,
		int gpuCount = 1)
	{
		DateTime startedAt = CreateTimestamp(minuteOffset);
		DateTime finishedAt = startedAt.AddMinutes(1);
		GpuTelemetryStatistics[] gpuStatistics = Enumerable.Range(0, gpuCount)
			.Select(index => new GpuTelemetryStatistics
			{
				Identifier = $"GPU-{index}",
				Name = $"GPU {index}",
				AvailableSampleCount = 4,
				UnavailableSampleCount = 1,
				LoadPercent = new MetricStatistics
				{
					SampleCount = 4,
					Minimum = 80 + index,
					Average = 90 + index,
					Maximum = 100
				}
			})
			.ToArray();
		GpuQualificationEvidence[] gpuEvidence = gpuStatistics
			.Select(statistics => new GpuQualificationEvidence
			{
				Identifier = statistics.Identifier,
				Name = statistics.Name,
				Result = ValidationStatus.Pass,
				ValidationMessage = "GPU qualification passed",
				TelemetryAvailable = true,
				TelemetryStatistics = statistics
			})
			.ToArray();
		SessionTelemetryStatistics telemetry = new()
		{
			CpuLoadPercent = new MetricStatistics
			{
				SampleCount = 4,
				Minimum = 80,
				Average = 90,
				Maximum = 100
			},
			SystemMemoryUsagePercent = new MetricStatistics
			{
				SampleCount = 4,
				Minimum = 40,
				Average = 50,
				Maximum = 60
			},
			Gpus = Array.AsReadOnly(gpuStatistics)
		};
		QualificationReport? report = includeReport
			? new QualificationReport
			{
				StartedAt = startedAt,
				FinishedAt = finishedAt,
				Duration = finishedAt - startedAt,
				Profile = QualificationProfiles.Standard,
				ExecutionMode = QualificationExecutionMode.Automated,
				CpuResult = ValidationStatus.Pass,
				MemoryResult = ValidationStatus.Pass,
				GpuResult = ValidationStatus.Pass,
				OverallResult = ValidationStatus.Pass,
				TelemetryStatistics = telemetry,
				GpuEvidence = Array.AsReadOnly(gpuEvidence)
			}
			: null;

		return new QualificationRecord
		{
			StartedAt = startedAt,
			FinishedAt = finishedAt,
			Duration = finishedAt - startedAt,
			Result = ValidationStatus.Pass,
			ExecutionMode = QualificationExecutionMode.Automated,
			ProfileName = QualificationProfiles.Standard.Name,
			TelemetryStatistics = telemetry,
			GpuEvidence = Array.AsReadOnly(gpuEvidence),
			Report = report
		};
	}

	private static DateTime CreateTimestamp(int minuteOffset)
	{
		return new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Local)
			.AddMinutes(minuteOffset);
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		public string Path { get; } =
			System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				$"Singularity.Tests.{Guid.NewGuid():N}");

		public string ArchivePath =>
			System.IO.Path.Combine(Path, QualificationArchiveService.ArchiveFileName);

		public TemporaryDirectory()
		{
			Directory.CreateDirectory(Path);
		}

		public void Dispose()
		{
			if (!Directory.Exists(Path))
				return;

			try
			{
				Directory.Delete(Path, recursive: true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	private sealed class IdleWorkloadController : IWorkloadController
	{
		public bool IsRunning => false;
		public WorkloadStatus Status { get; } = new();

		public void Start(WorkloadOptions options)
		{
		}

		public void Stop()
		{
		}

		public void ResetFailure()
		{
		}
	}
}
