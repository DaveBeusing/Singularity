// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

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
	private bool automatedRunFinalized;

	public QualificationSession Session { get; } = new();
	public QualificationHistory History { get; } = new();
	public ValidationResult? LastValidationResult { get; private set; }
	public QualificationReport? LastReport { get; private set; }
	public WorkloadStatus WorkloadStatus => workloadController.Status;
	public QualificationProgress Progress => qualificationRunner.Progress;

	public QualificationCoordinator(IWorkloadController workloadController)
	{
		this.workloadController = workloadController;
		qualificationRunner = new QualificationRunner(workloadController);
	}

	public bool StartManual(WorkloadOptions options, QualificationProfile profile)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(profile);

		if (workloadController.IsRunning || qualificationRunner.IsRunning)
			return false;

		workloadController.ResetFailure();
		qualificationRunner.Reset();
		PrepareSession(profile);
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
		PrepareSession(profile);
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

		if (!workloadController.IsRunning && workloadController.Status.State != WorkloadState.Failed)
			return false;

		workloadController.Stop();
		FinalizeSession();
		return true;
	}

	public void Update(SystemSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		if (workloadController.IsRunning)
		{
			Session.RecordTelemetry(snapshot);
			LastValidationResult = workloadValidator.Validate(
				workloadController.Status,
				snapshot,
				Session.Profile,
				Session.Duration);
		}

		if (qualificationRunner.IsRunning)
			qualificationRunner.Update(LastValidationResult);

		if (!automatedRunFinalized &&
			qualificationRunner.State is QualificationRunState.Completed or QualificationRunState.Failed)
		{
			automatedRunFinalized = true;
			FinalizeSession(qualificationRunner.State == QualificationRunState.Failed);
		}
	}

	private void PrepareSession(QualificationProfile profile)
	{
		LastValidationResult = null;
		LastReport = null;
		workloadValidator.Reset();
		Session.Start(profile);
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

		History.Add(Session);
		if (LastValidationResult is not null)
			LastReport = reportGenerator.Create(Session, LastValidationResult);
	}
}

