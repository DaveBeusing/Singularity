// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Persistence;
using Singularity.Core.Qualification;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Monitoring.Models;

namespace Singularity.Application;

public sealed class QualificationCoordinator
{
	private readonly IWorkloadController workloadController;
	private readonly QualificationRunner qualificationRunner;
	private readonly WorkloadValidator workloadValidator = new();
	private readonly QualificationReportGenerator reportGenerator = new();
	private readonly QualificationArchiveService? qualificationArchive;
	private readonly List<QualificationRecord> evidenceRecords = [];
	private readonly object archiveTaskLock = new();
	private Task archiveWriteTask = Task.CompletedTask;
	private bool automatedRunFinalized;
	private SystemSnapshot? lastSnapshot;

	public QualificationSession Session { get; } = new();
	public QualificationHistory History { get; } = new();
	public IReadOnlyList<QualificationRecord> EvidenceRecords => evidenceRecords;
	public ValidationResult? LastValidationResult { get; private set; }
	public QualificationReport? LastReport { get; private set; }
	public WorkloadStatus WorkloadStatus => workloadController.Status;
	public QualificationProgress Progress => qualificationRunner.Progress;
	public QualificationArchiveState ArchiveState =>
		qualificationArchive?.State ?? QualificationArchiveState.NotLoaded;
	public string? ArchiveError => qualificationArchive?.LastError;
	public string? ArchivePath => qualificationArchive?.ArchivePath;
	public int ArchivedRecordCount => qualificationArchive?.Records.Count ?? 0;

	public QualificationCoordinator(
		IWorkloadController workloadController,
		QualificationArchiveService? qualificationArchive = null)
	{
		this.workloadController = workloadController;
		this.qualificationArchive = qualificationArchive;
		qualificationRunner = new QualificationRunner(workloadController);
	}

	public async Task LoadArchiveAsync(CancellationToken cancellationToken = default)
	{
		if (qualificationArchive is null)
			return;

		await qualificationArchive.LoadAsync(cancellationToken);

		if (qualificationArchive.State == QualificationArchiveState.Ready)
		{
			ReplaceEvidenceRecords(
				evidenceRecords.Concat(qualificationArchive.Records));
		}
	}

	public async Task ClearArchiveAsync(CancellationToken cancellationToken = default)
	{
		await FlushArchiveAsync();

		if (qualificationArchive is not null)
		{
			await qualificationArchive.ClearAsync(cancellationToken);
			if (qualificationArchive.State != QualificationArchiveState.Ready)
				return;
		}

		History.Clear();
		evidenceRecords.Clear();
	}

	public Task FlushArchiveAsync()
	{
		lock (archiveTaskLock)
			return archiveWriteTask;
	}

	public bool StartManual(WorkloadOptions options, QualificationProfile profile)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(profile);

		if (workloadController.IsRunning || qualificationRunner.IsRunning)
			return false;

		workloadController.ResetFailure();
		qualificationRunner.Reset();
		PrepareSession(
			profile,
			QualificationExecutionMode.Manual,
			options.ResolveSelectedGpuIdentifiers());
		workloadController.Start(options);
		return true;
	}

	public bool StartAutomated(WorkloadOptions options, QualificationProfile profile)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(profile);

		if (workloadController.IsRunning || qualificationRunner.IsRunning)
			return false;

		qualificationRunner.Reset();
		QualificationPlan plan = QualificationPlan.CreateStandard(options, profile);
		PrepareSession(
			profile,
			QualificationExecutionMode.Automated,
			options.ResolveSelectedGpuIdentifiers());
		automatedRunFinalized = false;
		qualificationRunner.Start(plan);
		return true;
	}

	public bool Stop()
	{
		if (qualificationRunner.IsRunning)
		{
			qualificationRunner.Cancel();
			FinalizeSession(forceFailure: true);
			return true;
		}

		WorkloadStatus workloadStatus = workloadController.Status;
		WorkloadState workloadState = workloadStatus.State;
		if (!workloadController.IsRunning && workloadState != WorkloadState.Failed)
			return false;

		if (workloadState == WorkloadState.Failed &&
			workloadStatus.GpuEnabled &&
			workloadStatus.GpuDevices.Count > 0)
		{
			LastValidationResult = lastSnapshot is null
				? null
				: workloadValidator.Validate(
					workloadStatus,
					lastSnapshot,
					Session.Profile,
					Session.Duration);
		}

		workloadController.Stop();
		FinalizeSession(forceFailure: workloadState == WorkloadState.Failed);
		return true;
	}

	public void Update(SystemSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		lastSnapshot = snapshot;

		WorkloadStatus workloadStatus = workloadController.Status;
		bool hasGpuFailureEvidence =
			workloadStatus.State == WorkloadState.Failed &&
			workloadStatus.GpuEnabled &&
			workloadStatus.GpuDevices.Count > 0;

		if (workloadStatus.IsRunning || hasGpuFailureEvidence)
		{
			Session.RecordTelemetry(snapshot);
			LastValidationResult = workloadValidator.Validate(
				workloadStatus,
				snapshot,
				Session.Profile,
				Session.Duration);
		}

		if (qualificationRunner.IsRunning)
			qualificationRunner.Update(LastValidationResult);

		if (Session.State == QualificationSessionState.Running &&
			qualificationRunner.State == QualificationRunState.Idle &&
			workloadStatus.State == WorkloadState.Failed)
		{
			FinalizeSession(forceFailure: true);
		}

		if (!automatedRunFinalized &&
			qualificationRunner.State is QualificationRunState.Completed or QualificationRunState.Failed)
		{
			automatedRunFinalized = true;
			FinalizeSession(qualificationRunner.State == QualificationRunState.Failed);
		}
	}

	private void PrepareSession(
		QualificationProfile profile,
		QualificationExecutionMode executionMode,
		IReadOnlyList<string> selectedGpuIdentifiers)
	{
		LastValidationResult = null;
		LastReport = null;
		lastSnapshot = null;
		workloadValidator.Reset();
		Session.Start(profile, executionMode, selectedGpuIdentifiers);
	}

	private void FinalizeSession(bool forceFailure = false)
	{
		if (Session.State != QualificationSessionState.Running)
			return;

		if (forceFailure || LastValidationResult is null)
			Session.Fail();
		else
			Session.Complete(new ValidationSummary(LastValidationResult).OverallStatus);

		if (!Session.CanBeRecorded)
			return;

		LastReport = LastValidationResult is null
			? null
			: reportGenerator.Create(Session, LastValidationResult);

		QualificationRecord? record = History.Add(Session, LastReport);
		if (record is null)
			return;

		AddEvidenceRecord(record);
		QueueArchiveSave(record);
	}

	private void AddEvidenceRecord(QualificationRecord record)
	{
		QualificationRecordKey key = CreateRecordKey(record);
		evidenceRecords.RemoveAll(item => CreateRecordKey(item) == key);
		evidenceRecords.Add(record);
		evidenceRecords.Sort(
			(left, right) =>
			{
				int finished = right.FinishedAt.CompareTo(left.FinishedAt);
				return finished != 0
					? finished
					: right.StartedAt.CompareTo(left.StartedAt);
			});

		while (evidenceRecords.Count > QualificationArchiveService.DefaultRetentionLimit)
			evidenceRecords.RemoveAt(evidenceRecords.Count - 1);
	}

	private void ReplaceEvidenceRecords(IEnumerable<QualificationRecord> records)
	{
		evidenceRecords.Clear();
		foreach (QualificationRecord record in records
			.GroupBy(CreateRecordKey)
			.Select(group => group.First())
			.OrderByDescending(item => item.FinishedAt)
			.ThenByDescending(item => item.StartedAt)
			.Take(QualificationArchiveService.DefaultRetentionLimit))
		{
			evidenceRecords.Add(record);
		}
	}

	private void QueueArchiveSave(QualificationRecord record)
	{
		if (qualificationArchive is null)
			return;

		lock (archiveTaskLock)
		{
			archiveWriteTask = PersistAfterAsync(archiveWriteTask, record);
		}
	}

	private async Task PersistAfterAsync(Task previousWrite, QualificationRecord record)
	{
		await previousWrite.ConfigureAwait(false);
		if (qualificationArchive is not null)
			await qualificationArchive.SaveAsync(record).ConfigureAwait(false);
	}

	private static QualificationRecordKey CreateRecordKey(QualificationRecord record)
	{
		return new QualificationRecordKey(
			record.StartedAt,
			record.FinishedAt,
			record.ProfileName,
			record.ExecutionMode);
	}

	private readonly record struct QualificationRecordKey(
		DateTime StartedAt,
		DateTime FinishedAt,
		string ProfileName,
		QualificationExecutionMode ExecutionMode);
}
