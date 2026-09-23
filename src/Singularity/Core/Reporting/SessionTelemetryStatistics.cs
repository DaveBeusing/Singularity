// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Monitoring.Models;

namespace Singularity.Core.Reporting;

public sealed class SessionTelemetryStatistics
{
	public static SessionTelemetryStatistics Empty { get; } = new();

	public MetricStatistics? CpuLoadPercent { get; init; }
	public MetricStatistics? GpuLoadPercent { get; init; }
	public MetricStatistics? GpuTemperatureCelsius { get; init; }
	public MetricStatistics? GpuPowerWatts { get; init; }
	public MetricStatistics? GpuVramUsagePercent { get; init; }
	public MetricStatistics? SystemMemoryUsagePercent { get; init; }

	public IReadOnlyList<GpuTelemetryStatistics> Gpus { get; init; } =
		Array.Empty<GpuTelemetryStatistics>();
}

internal sealed class SessionTelemetryCollector
{
	private readonly MetricAccumulator cpuLoad = new();
	private readonly MetricAccumulator gpuLoad = new();
	private readonly MetricAccumulator gpuTemperature = new();
	private readonly MetricAccumulator gpuPower = new();
	private readonly MetricAccumulator gpuVram = new();
	private readonly MetricAccumulator systemMemory = new();
	private readonly IReadOnlyList<string> selectedGpuIdentifiers;
	private readonly Dictionary<string, GpuTelemetryAccumulator> gpuDevices =
		new(StringComparer.OrdinalIgnoreCase);

	public SessionTelemetryCollector()
		: this(Array.Empty<string>())
	{
	}

	public SessionTelemetryCollector(IReadOnlyList<string> selectedGpuIdentifiers)
	{
		ArgumentNullException.ThrowIfNull(selectedGpuIdentifiers);

		this.selectedGpuIdentifiers = Array.AsReadOnly(
			selectedGpuIdentifiers
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());

		foreach (string identifier in this.selectedGpuIdentifiers)
			gpuDevices.Add(identifier, new GpuTelemetryAccumulator(identifier));
	}

	public void Add(SystemSnapshot snapshot)
	{
		cpuLoad.Add(snapshot.CpuLoadPercent);
		systemMemory.Add(snapshot.UsedPhysicalMemoryPercent);

		if (selectedGpuIdentifiers.Count > 0)
		{
			foreach (string identifier in selectedGpuIdentifiers)
			{
				GpuTelemetrySnapshot? gpu = snapshot.FindGpuTelemetry(identifier);
				gpuDevices[identifier].Add(gpu);
			}

			if (selectedGpuIdentifiers.Count == 1)
			{
				GpuTelemetrySnapshot? gpu =
					snapshot.FindGpuTelemetry(selectedGpuIdentifiers[0]);
				AddLegacyGpuMetrics(gpu);
			}

			return;
		}

		if (!snapshot.GpuTelemetryAvailable)
			return;

		gpuLoad.Add(snapshot.GpuLoadPercent);
		gpuTemperature.Add(snapshot.GpuTemperatureCelsius);
		gpuVram.Add(snapshot.GpuMemoryUsedPercent);

		if (snapshot.GpuPowerAvailable)
			gpuPower.Add(snapshot.GpuPowerWatts);
	}

	public SessionTelemetryStatistics Snapshot()
	{
		GpuTelemetryStatistics[] perDevice = selectedGpuIdentifiers
			.Select(identifier => gpuDevices[identifier].Snapshot())
			.ToArray();

		return new SessionTelemetryStatistics
		{
			CpuLoadPercent = cpuLoad.Snapshot(),
			GpuLoadPercent = gpuLoad.Snapshot(),
			GpuTemperatureCelsius = gpuTemperature.Snapshot(),
			GpuPowerWatts = gpuPower.Snapshot(),
			GpuVramUsagePercent = gpuVram.Snapshot(),
			SystemMemoryUsagePercent = systemMemory.Snapshot(),
			Gpus = Array.AsReadOnly(perDevice)
		};
	}

	private void AddLegacyGpuMetrics(GpuTelemetrySnapshot? gpu)
	{
		if (gpu is null || !gpu.IsAvailable)
			return;

		gpuLoad.Add(gpu.LoadPercent);
		gpuTemperature.Add(gpu.TemperatureCelsius);
		gpuVram.Add(gpu.MemoryUsedPercent);

		if (gpu.PowerAvailable)
			gpuPower.Add(gpu.PowerWatts);
	}

	private sealed class GpuTelemetryAccumulator
	{
		private readonly MetricAccumulator load = new();
		private readonly MetricAccumulator temperature = new();
		private readonly MetricAccumulator power = new();
		private readonly MetricAccumulator vram = new();
		private long availableSampleCount;
		private long unavailableSampleCount;
		private string name = string.Empty;

		public string Identifier { get; }

		public GpuTelemetryAccumulator(string identifier)
		{
			Identifier = identifier;
		}

		public void Add(GpuTelemetrySnapshot? snapshot)
		{
			if (snapshot is null || !snapshot.IsAvailable)
			{
				unavailableSampleCount++;
				if (snapshot is not null && !string.IsNullOrWhiteSpace(snapshot.Name))
					name = snapshot.Name;
				return;
			}

			availableSampleCount++;
			if (!string.IsNullOrWhiteSpace(snapshot.Name))
				name = snapshot.Name;
			load.Add(snapshot.LoadPercent);
			temperature.Add(snapshot.TemperatureCelsius);
			vram.Add(snapshot.MemoryUsedPercent);
			if (snapshot.PowerAvailable)
				power.Add(snapshot.PowerWatts);
		}

		public GpuTelemetryStatistics Snapshot()
		{
			return new GpuTelemetryStatistics
			{
				Identifier = Identifier,
				Name = name,
				AvailableSampleCount = availableSampleCount,
				UnavailableSampleCount = unavailableSampleCount,
				LoadPercent = load.Snapshot(),
				TemperatureCelsius = temperature.Snapshot(),
				PowerWatts = power.Snapshot(),
				VramUsagePercent = vram.Snapshot()
			};
		}
	}
}
