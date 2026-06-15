namespace Singularity.Workloads;

/// <summary>
/// Platzhalter für spätere GPU-Last.
/// Echte GPU-Maximallast benötigt Direct3D, CUDA, OpenCL oder Vulkan.
/// In Version 1 bleibt dieses Modul bewusst vorbereitet,
/// damit die Architektur bereits sauber erweitert werden kann.
/// </summary>
public sealed class GpuWorkload : IWorkload
{
	public string Name => "GPU";
	public bool IsRunning { get; private set; }
	public void Start(CancellationToken token)
	{
		IsRunning = true;

		Task.Run(() =>
		{
			while (!token.IsCancellationRequested)
			{
				Thread.Sleep(250);
			}
		}, token);
	}
	public void Stop()
	{
		IsRunning = false;
	}
}