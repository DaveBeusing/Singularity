// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed class WorkloadManager : IWorkloadController, IDisposable
{
	private const string DefaultGpuWorkerKey = "__default__";

	private readonly CpuStressWorker cpuStressWorker = new();
	private readonly MemoryStressWorker memoryStressWorker = new();
	private readonly Dictionary<string, GpuStressWorker> gpuStressWorkers =
		new(StringComparer.OrdinalIgnoreCase);

	private WorkloadState state = WorkloadState.Stopped;
	private string message = "Ready";

	private bool cpuEnabled;
	private bool memoryEnabled;
	private bool gpuEnabled;
	private int cpuThreads;
	private int memoryGb;
	private int gpuLoadPercent;
	private IReadOnlyList<string> selectedGpuIdentifiers = Array.Empty<string>();
	private IReadOnlyList<GpuWorkloadDeviceStatus> terminalGpuDeviceStatuses =
		Array.Empty<GpuWorkloadDeviceStatus>();

	public bool IsRunning
	{
		get
		{
			RefreshGpuState();
			return state is WorkloadState.Starting or WorkloadState.Running or WorkloadState.Stopping;
		}
	}

	public WorkloadStatus Status
	{
		get
		{
			RefreshGpuState();
			return new WorkloadStatus
			{
				State = state,
				CpuEnabled = cpuEnabled,
				MemoryEnabled = memoryEnabled,
				GpuEnabled = gpuEnabled,
				CpuThreads = cpuThreads,
				MemoryGb = memoryGb,
				GpuLoadPercent = gpuLoadPercent,
				SelectedGpuIdentifier = selectedGpuIdentifiers.Count == 1
					? selectedGpuIdentifiers[0]
					: null,
				SelectedGpuIdentifiers = selectedGpuIdentifiers,
				GpuDevices = CreateGpuDeviceStatuses(),
				MemoryAllocatedMb = memoryStressWorker.AllocatedMegabytes,
				Message = message
			};
		}
	}

	public void Start(WorkloadOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (IsRunning)
			return;

		state = WorkloadState.Starting;
		message = "Starting";
		cpuEnabled = options.EnableCpuWorkload;
		memoryEnabled = options.EnableMemoryWorkload;
		gpuEnabled = options.EnableGpuWorkload;
		cpuThreads = options.CpuThreads;
		memoryGb = options.MemoryGb;
		gpuLoadPercent = options.GpuLoadPercent;
		selectedGpuIdentifiers = options.ResolveSelectedGpuIdentifiers();

		try
		{
			if (cpuEnabled)
				cpuStressWorker.Start(cpuThreads);
			if (memoryEnabled)
				memoryStressWorker.Start(memoryGb);
			if (gpuEnabled)
				StartGpuWorkers();

			state = gpuEnabled ? WorkloadState.Starting : WorkloadState.Running;
			message = BuildRunningMessage();
		}
		catch (Exception ex)
		{
			StopWorkers();
			state = WorkloadState.Failed;
			message = ex.Message;
		}
	}

	public void Stop()
	{
		if (state == WorkloadState.Stopped)
			return;

		state = WorkloadState.Stopping;
		message = "Stopping";
		StopWorkers();
		state = WorkloadState.Stopped;
		message = "Ready";
		ResetConfiguration();
	}

	public void ResetFailure()
	{
		if (state != WorkloadState.Failed)
			return;

		state = WorkloadState.Stopped;
		message = "Ready";
		ResetConfiguration();
	}

	private void StartGpuWorkers()
	{
		StopGpuWorkers();
		terminalGpuDeviceStatuses = Array.Empty<GpuWorkloadDeviceStatus>();

		if (selectedGpuIdentifiers.Count == 0)
		{
			GpuStressWorker defaultWorker = new();
			gpuStressWorkers.Add(DefaultGpuWorkerKey, defaultWorker);
			defaultWorker.Start(gpuLoadPercent);
			return;
		}

		foreach (string identifier in selectedGpuIdentifiers)
		{
			GpuStressWorker worker = new();
			gpuStressWorkers.Add(identifier, worker);
			worker.Start(gpuLoadPercent, identifier);
		}
	}

	private void StopWorkers()
	{
		cpuStressWorker.Stop();
		memoryStressWorker.Stop();
		StopGpuWorkers();
	}

	private void StopGpuWorkers()
	{
		foreach (GpuStressWorker worker in gpuStressWorkers.Values)
		{
			worker.Stop();
			worker.Dispose();
		}

		gpuStressWorkers.Clear();
	}

	private void ResetConfiguration()
	{
		cpuEnabled = false;
		memoryEnabled = false;
		gpuEnabled = false;
		cpuThreads = 0;
		memoryGb = 0;
		gpuLoadPercent = 0;
		selectedGpuIdentifiers = Array.Empty<string>();
		terminalGpuDeviceStatuses = Array.Empty<GpuWorkloadDeviceStatus>();
	}

	private string BuildRunningMessage()
	{
		List<string> parts = [];
		if (cpuEnabled)
			parts.Add($"CPU {cpuThreads}T");
		if (memoryEnabled)
			parts.Add($"RAM {memoryGb}GB");
		if (gpuEnabled)
		{
			string gpuTarget = selectedGpuIdentifiers.Count > 1
				? $"GPU x{selectedGpuIdentifiers.Count} {gpuLoadPercent}%"
				: $"GPU {gpuLoadPercent}%";
			parts.Add(gpuTarget);
		}

		return parts.Count == 0 ? "No workload selected" : string.Join(" | ", parts);
	}

	public void Dispose()
	{
		StopWorkers();
		cpuStressWorker.Dispose();
		memoryStressWorker.Dispose();
		state = WorkloadState.Stopped;
		ResetConfiguration();
	}

	private void RefreshGpuState()
	{
		if (!gpuEnabled || state is WorkloadState.Stopped or WorkloadState.Stopping or WorkloadState.Failed)
			return;

		foreach ((string key, GpuStressWorker worker) in gpuStressWorkers)
		{
			if (worker.Failure is not Exception failure)
				continue;

			string failureMessage = key == DefaultGpuWorkerKey
				? failure.Message
				: $"GPU {key} failed: {failure.Message}";
			terminalGpuDeviceStatuses = CreateTerminalGpuDeviceStatuses(key, failureMessage);
			cpuStressWorker.Stop();
			memoryStressWorker.Stop();
			StopGpuWorkers();
			state = WorkloadState.Failed;
			message = failureMessage;
			return;
		}

		if (state == WorkloadState.Starting &&
			gpuStressWorkers.Count > 0 &&
			gpuStressWorkers.Values.All(worker => worker.IsReady))
		{
			state = WorkloadState.Running;
			message = BuildRunningMessage();
		}
	}

	private IReadOnlyList<GpuWorkloadDeviceStatus> CreateGpuDeviceStatuses()
	{
		if (!gpuEnabled || selectedGpuIdentifiers.Count == 0)
			return Array.Empty<GpuWorkloadDeviceStatus>();
		if (state == WorkloadState.Failed && terminalGpuDeviceStatuses.Count > 0)
			return terminalGpuDeviceStatuses;

		GpuWorkloadDeviceStatus[] statuses = new GpuWorkloadDeviceStatus[selectedGpuIdentifiers.Count];
		for (int index = 0; index < selectedGpuIdentifiers.Count; index++)
		{
			string identifier = selectedGpuIdentifiers[index];
			if (!gpuStressWorkers.TryGetValue(identifier, out GpuStressWorker? worker))
			{
				statuses[index] = new GpuWorkloadDeviceStatus(
					identifier,
					state == WorkloadState.Failed ? WorkloadState.Failed : WorkloadState.Stopped,
					state == WorkloadState.Failed ? message : "Stopped");
				continue;
			}

			WorkloadState deviceState = worker.Failure is not null
				? WorkloadState.Failed
				: worker.IsReady
					? WorkloadState.Running
					: WorkloadState.Starting;
			statuses[index] = new GpuWorkloadDeviceStatus(
				identifier,
				deviceState,
				worker.Failure?.Message ?? (worker.IsReady ? "Running" : "Starting"));
		}

		return Array.AsReadOnly(statuses);
	}

	private IReadOnlyList<GpuWorkloadDeviceStatus> CreateTerminalGpuDeviceStatuses(
		string failedKey,
		string failureMessage)
	{
		if (failedKey == DefaultGpuWorkerKey || selectedGpuIdentifiers.Count == 0)
			return Array.Empty<GpuWorkloadDeviceStatus>();

		GpuWorkloadDeviceStatus[] statuses = new GpuWorkloadDeviceStatus[selectedGpuIdentifiers.Count];
		for (int index = 0; index < selectedGpuIdentifiers.Count; index++)
		{
			string identifier = selectedGpuIdentifiers[index];
			bool failed = string.Equals(identifier, failedKey, StringComparison.OrdinalIgnoreCase);
			statuses[index] = new GpuWorkloadDeviceStatus(
				identifier,
				failed ? WorkloadState.Failed : WorkloadState.Stopped,
				failed ? failureMessage : "Stopped after another GPU workload failed");
		}

		return Array.AsReadOnly(statuses);
	}
}
