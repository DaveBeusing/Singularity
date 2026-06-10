using Singularity.Workloads;

namespace Singularity.Core;

/// <summary>
/// Zentrale Steuerklasse für alle Workloads.
/// Der Controller weiß, welche Module aktiv sind,
/// startet sie gemeinsam und stoppt sie gemeinsam.
/// </summary>
public sealed class WorkloadController
{
	private readonly List<IWorkload> workloads = new();

	private CancellationTokenSource? cancellation;

	public bool IsRunning { get; private set; }

	public IReadOnlyList<IWorkload> Workloads => workloads;

	public void Configure(WorkloadOptions options)
	{
		if (IsRunning)
			throw new InvalidOperationException("Cannot configure while stress test is running.");

		workloads.Clear();

		if (options.EnableCpuWorkload)
		{
			workloads.Add(new CpuWorkload(options.CpuThreads));
		}

		if (options.EnableMemoryWorkload)
		{
			workloads.Add(new MemoryWorkload(options.MemoryGb));
		}

		if (options.EnableGpuWorkload)
		{
			workloads.Add(new GpuWorkload());
		}
	}

	public void Start()
	{
		if (IsRunning)
			return;

		cancellation = new CancellationTokenSource();

		foreach (IWorkload workload in workloads)
		{
			workload.Start(cancellation.Token);
		}

		IsRunning = true;
	}

	public void Stop()
	{
		if (!IsRunning)
			return;

		cancellation?.Cancel();

		foreach (IWorkload workload in workloads)
		{
			workload.Stop();
		}

		IsRunning = false;
	}
}