// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;

namespace Singularity.Core.Qualification;

public sealed class QualificationRunner
{
	private readonly WorkloadManager workloadManager;
	private readonly Stopwatch stepTimer = new();
	private QualificationPlan? plan;
	private int stepIndex = -1;

	public QualificationRunState State { get; private set; } = QualificationRunState.Idle;
	public bool IsRunning => State == QualificationRunState.Running;
	public QualificationProgress Progress => CreateProgress();

	public QualificationRunner(WorkloadManager workloadManager)
	{
		this.workloadManager = workloadManager;
	}

	public void Start(QualificationPlan qualificationPlan)
	{
		if (IsRunning)
			throw new InvalidOperationException("A qualification run is already active.");
		if (qualificationPlan.Steps.Count == 0)
			throw new ArgumentException("A qualification plan must contain at least one step.", nameof(qualificationPlan));

		plan = qualificationPlan;
		stepIndex = 0;
		State = QualificationRunState.Running;
		StartCurrentStep();
	}

	public void Update(ValidationResult? validation)
	{
		if (!IsRunning || plan is null)
			return;

		WorkloadStatus status = workloadManager.Status;
		if (status.State == WorkloadState.Failed)
		{
			Finish(QualificationRunState.Failed);
			return;
		}

		if (plan.StopOnFailure && validation is not null && !validation.IsSuccess)
		{
			Finish(QualificationRunState.Failed);
			return;
		}

		if (stepTimer.Elapsed < plan.Steps[stepIndex].Duration)
			return;

		workloadManager.Stop();
		stepIndex++;
		if (stepIndex >= plan.Steps.Count)
		{
			Finish(QualificationRunState.Completed);
			return;
		}
		StartCurrentStep();
	}

	public void Cancel()
	{
		if (IsRunning)
			Finish(QualificationRunState.Cancelled);
	}

	public void Reset()
	{
		if (IsRunning)
			throw new InvalidOperationException("Cancel the active qualification before resetting it.");
		plan = null;
		stepIndex = -1;
		stepTimer.Reset();
		State = QualificationRunState.Idle;
	}

	private void StartCurrentStep()
	{
		if (plan is null)
			return;
		workloadManager.ResetFailure();
		workloadManager.Start(plan.Steps[stepIndex].Workload);
		stepTimer.Restart();
	}

	private void Finish(QualificationRunState finalState)
	{
		workloadManager.Stop();
		stepTimer.Stop();
		State = finalState;
	}

	private QualificationProgress CreateProgress()
	{
		if (plan is null || stepIndex < 0)
			return new QualificationProgress(State, string.Empty, 0, 0, TimeSpan.Zero, TimeSpan.Zero);
		int currentIndex = Math.Min(stepIndex, plan.Steps.Count - 1);
		QualificationStep step = plan.Steps[currentIndex];
		return new QualificationProgress(State, step.Name, currentIndex + 1, plan.Steps.Count, stepTimer.Elapsed, step.Duration);
	}
}
