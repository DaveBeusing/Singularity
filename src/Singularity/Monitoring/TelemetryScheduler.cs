// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Singularity.Monitoring;

internal sealed class TelemetryScheduler : IDisposable
{
	private static readonly TimeSpan SchedulerResolution = TimeSpan.FromMilliseconds(100);
	private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(500);
	private static readonly TimeSpan MediumInterval = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan SlowInterval = TimeSpan.FromSeconds(2);

	private readonly CancellationTokenSource cancellation = new();
	private readonly Task samplingTask;
	private readonly Action sampleFast;
	private readonly Action sampleMedium;
	private readonly Action sampleSlow;
	private bool disposed;

	public TelemetryScheduler(
		Action sampleFast,
		Action sampleMedium,
		Action sampleSlow)
	{
		this.sampleFast = sampleFast;
		this.sampleMedium = sampleMedium;
		this.sampleSlow = sampleSlow;
		samplingTask = Task.Run(() => RunAsync(cancellation.Token));
	}

	private async Task RunAsync(CancellationToken cancellationToken)
	{
		using PeriodicTimer timer = new(SchedulerResolution);
		Stopwatch stopwatch = Stopwatch.StartNew();
		TimeSpan nextFast = TimeSpan.Zero;
		TimeSpan nextMedium = TimeSpan.Zero;
		TimeSpan nextSlow = TimeSpan.Zero;

		try
		{
			do
			{
				TimeSpan elapsed = stopwatch.Elapsed;

				if (elapsed >= nextFast)
				{
					RunSample(sampleFast);
					nextFast = elapsed + FastInterval;
				}

				if (elapsed >= nextMedium)
				{
					RunSample(sampleMedium);
					nextMedium = elapsed + MediumInterval;
				}

				if (elapsed >= nextSlow)
				{
					RunSample(sampleSlow);
					nextSlow = elapsed + SlowInterval;
				}
			}
			while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Expected during application shutdown.
		}
	}

	private static void RunSample(Action sample)
	{
		try
		{
			sample();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Telemetry sampling error: {ex}");
		}
	}

	public void Dispose()
	{
		if (disposed)
			return;

		cancellation.Cancel();

		try
		{
			samplingTask.GetAwaiter().GetResult();
		}
		catch (OperationCanceledException)
		{
			// Expected during application shutdown.
		}

		cancellation.Dispose();
		disposed = true;
	}
}
