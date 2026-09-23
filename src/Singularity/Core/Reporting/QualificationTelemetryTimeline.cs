// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Monitoring.Models;

namespace Singularity.Core.Reporting;

public enum QualificationTimelineEventKind
{
	Start,
	StepTransition,
	Stop,
	Cancelled,
	Completed,
	Failed
}

public sealed class QualificationTelemetryGpuPoint
{
	public string Identifier { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public double? LoadPercent { get; init; }
	public double? TemperatureCelsius { get; init; }
	public double? PowerWatts { get; init; }
	public double? VramUsagePercent { get; init; }
}

public sealed class QualificationTelemetryPoint
{
	public TimeSpan Elapsed { get; init; }
	public double? CpuLoadPercent { get; init; }
	public double? SystemMemoryUsagePercent { get; init; }
	public IReadOnlyList<QualificationTelemetryGpuPoint> Gpus { get; init; } =
		Array.Empty<QualificationTelemetryGpuPoint>();
}

public sealed class QualificationTimelineEvent
{
	public TimeSpan Elapsed { get; init; }
	public QualificationTimelineEventKind Kind { get; init; }
	public string Label { get; init; } = string.Empty;
}

public sealed class QualificationTelemetryTimeline
{
	public const int CurrentSchemaVersion = 1;
	public const int DefaultMaximumPoints = 720;
	public const int MaximumEvents = 128;

	public static QualificationTelemetryTimeline Empty { get; } = new();

	public int SchemaVersion { get; init; } = CurrentSchemaVersion;
	public int MaximumPoints { get; init; } = DefaultMaximumPoints;
	public TimeSpan SamplingInterval { get; init; }
	public IReadOnlyList<QualificationTelemetryPoint> Points { get; init; } =
		Array.Empty<QualificationTelemetryPoint>();
	public IReadOnlyList<QualificationTimelineEvent> Events { get; init; } =
		Array.Empty<QualificationTimelineEvent>();
}

internal sealed class QualificationTelemetryTimelineCollector
{
	private static readonly TimeSpan MinimumSamplingInterval = TimeSpan.FromMilliseconds(500);

	private readonly int maximumPoints;
	private readonly IReadOnlyList<string> selectedGpuIdentifiers;
	private readonly List<QualificationTelemetryPoint> points = [];
	private readonly List<QualificationTimelineEvent> events = [];
	private readonly Dictionary<string, ExtremePair> extrema = new(StringComparer.OrdinalIgnoreCase);
	private TimeSpan samplingInterval;

	public TimeSpan LastElapsed => points.Count == 0 ? TimeSpan.Zero : points[^1].Elapsed;

	public QualificationTelemetryTimelineCollector(
		TimeSpan expectedDuration = default,
		IReadOnlyList<string>? selectedGpuIdentifiers = null,
		int maximumPoints = QualificationTelemetryTimeline.DefaultMaximumPoints)
	{
		if (maximumPoints < 8)
			throw new ArgumentOutOfRangeException(nameof(maximumPoints), "Timeline budget must contain at least eight points.");

		this.maximumPoints = maximumPoints;
		this.selectedGpuIdentifiers = Array.AsReadOnly(
			(selectedGpuIdentifiers ?? Array.Empty<string>())
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());
		samplingInterval = CalculateInitialInterval(expectedDuration, maximumPoints);
	}

	public void Add(SystemSnapshot snapshot, TimeSpan elapsed)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		if (elapsed < TimeSpan.Zero)
			elapsed = TimeSpan.Zero;
		if (elapsed < LastElapsed)
			elapsed = LastElapsed;

		QualificationTelemetryPoint point = CreatePoint(snapshot, elapsed);
		UpdateExtrema(point);

		if (points.Count == 0)
		{
			points.Add(point);
			return;
		}

		if (points.Count == 1 && points[0].Elapsed == TimeSpan.Zero && elapsed > TimeSpan.Zero)
		{
			points.Add(point);
			return;
		}

		if (elapsed - points[^1].Elapsed < samplingInterval)
		{
			points[^1] = point;
			return;
		}

		if (points.Count >= maximumPoints)
			Coarsen();

		points.Add(point);
		if (points.Count > maximumPoints)
			points.RemoveAt(Math.Max(1, points.Count - 2));
	}

	public void AddEvent(TimeSpan elapsed, QualificationTimelineEventKind kind, string label)
	{
		if (elapsed < TimeSpan.Zero)
			elapsed = TimeSpan.Zero;
		if (events.Count > 0 && elapsed < events[^1].Elapsed)
			elapsed = events[^1].Elapsed;

		QualificationTimelineEvent marker = new()
		{
			Elapsed = elapsed,
			Kind = kind,
			Label = label
		};

		if (events.Count >= QualificationTelemetryTimeline.MaximumEvents)
		{
			int removable = events.FindIndex(item => item.Kind != QualificationTimelineEventKind.Start);
			if (removable >= 0)
				events.RemoveAt(removable);
			else
				return;
		}

		events.Add(marker);
	}

	public QualificationTelemetryTimeline Snapshot()
	{
		QualificationTelemetryPoint[] frozenPoints = SelectFrozenPoints();
		QualificationTimelineEvent[] frozenEvents = events
			.OrderBy(item => item.Elapsed)
			.ToArray();

		return new QualificationTelemetryTimeline
		{
			MaximumPoints = maximumPoints,
			SamplingInterval = samplingInterval,
			Points = Array.AsReadOnly(frozenPoints),
			Events = Array.AsReadOnly(frozenEvents)
		};
	}

	private QualificationTelemetryPoint CreatePoint(SystemSnapshot snapshot, TimeSpan elapsed)
	{
		IEnumerable<GpuTelemetrySnapshot?> gpus = selectedGpuIdentifiers.Count > 0
			? selectedGpuIdentifiers.Select(snapshot.FindGpuTelemetry)
			: snapshot.GpuTelemetrySnapshots.Cast<GpuTelemetrySnapshot?>();

		QualificationTelemetryGpuPoint[] gpuPoints = gpus
			.Select((gpu, index) => CreateGpuPoint(
				gpu,
				gpu?.Identifier ??
					(selectedGpuIdentifiers.Count > index ? selectedGpuIdentifiers[index] : string.Empty)))
			.Where(gpu => !string.IsNullOrWhiteSpace(gpu.Identifier))
			.ToArray();

		return new QualificationTelemetryPoint
		{
			Elapsed = elapsed,
			CpuLoadPercent = Finite(snapshot.CpuLoadPercent),
			SystemMemoryUsagePercent = Finite(snapshot.UsedPhysicalMemoryPercent),
			Gpus = Array.AsReadOnly(gpuPoints)
		};
	}

	private static QualificationTelemetryGpuPoint CreateGpuPoint(
		GpuTelemetrySnapshot? snapshot,
		string identifier)
	{
		bool available = snapshot?.IsAvailable == true;
		return new QualificationTelemetryGpuPoint
		{
			Identifier = identifier,
			Name = snapshot?.Name ?? string.Empty,
			LoadPercent = available ? Finite(snapshot!.LoadPercent) : null,
			TemperatureCelsius = available ? snapshot!.TemperatureCelsius : null,
			PowerWatts = available && snapshot!.PowerAvailable ? Finite(snapshot.PowerWatts) : null,
			VramUsagePercent = available && snapshot!.MemoryTotalBytes > 0
				? Finite(snapshot.MemoryUsedPercent)
				: null
		};
	}

	private void Coarsen()
	{
		samplingInterval = TimeSpan.FromTicks(Math.Max(
			MinimumSamplingInterval.Ticks,
			samplingInterval.Ticks * 2));

		List<QualificationTelemetryPoint> compacted = new(maximumPoints);
		compacted.Add(points[0]);
		long lastBucket = -1;
		for (int index = 1; index < points.Count; index++)
		{
			QualificationTelemetryPoint point = points[index];
			long bucket = point.Elapsed.Ticks / Math.Max(1, samplingInterval.Ticks);
			if (bucket == lastBucket && compacted.Count > 1)
				compacted[^1] = point;
			else
			{
				compacted.Add(point);
				lastBucket = bucket;
			}
		}

		points.Clear();
		points.AddRange(compacted.Take(maximumPoints - 1));
	}

	private void UpdateExtrema(QualificationTelemetryPoint point)
	{
		Track("cpu", point.CpuLoadPercent, point);
		Track("memory", point.SystemMemoryUsagePercent, point);
		foreach (QualificationTelemetryGpuPoint gpu in point.Gpus)
		{
			string prefix = $"gpu:{gpu.Identifier}:";
			Track(prefix + "load", gpu.LoadPercent, point);
			Track(prefix + "temperature", gpu.TemperatureCelsius, point);
			Track(prefix + "power", gpu.PowerWatts, point);
			Track(prefix + "vram", gpu.VramUsagePercent, point);
		}
	}

	private void Track(string key, double? value, QualificationTelemetryPoint point)
	{
		if (value is null)
			return;

		if (!extrema.TryGetValue(key, out ExtremePair? pair))
		{
			pair = new ExtremePair();
			extrema.Add(key, pair);
		}

		if (pair.Minimum is null || value < pair.Minimum)
		{
			pair.Minimum = value;
			pair.MinimumPoint = point;
		}
		if (pair.Maximum is null || value > pair.Maximum)
		{
			pair.Maximum = value;
			pair.MaximumPoint = point;
		}
	}

	private QualificationTelemetryPoint[] SelectFrozenPoints()
	{
		if (points.Count == 0)
			return [];

		HashSet<long> protectedTicks =
		[
			points[0].Elapsed.Ticks,
			points[^1].Elapsed.Ticks
		];
		foreach (ExtremePair pair in extrema.Values)
		{
			if (pair.MinimumPoint is not null)
				protectedTicks.Add(pair.MinimumPoint.Elapsed.Ticks);
			if (pair.MaximumPoint is not null)
				protectedTicks.Add(pair.MaximumPoint.Elapsed.Ticks);
		}

		QualificationTelemetryPoint[] candidates = points
			.Concat(extrema.Values.SelectMany(pair =>
				new[] { pair.MinimumPoint, pair.MaximumPoint }))
			.Where(point => point is not null)
			.Cast<QualificationTelemetryPoint>()
			.GroupBy(point => point.Elapsed.Ticks)
			.Select(group => group.Last())
			.OrderBy(point => point.Elapsed)
			.ToArray();

		if (candidates.Length <= maximumPoints)
			return candidates;

		List<QualificationTelemetryPoint> selected = candidates
			.Where(point => protectedTicks.Contains(point.Elapsed.Ticks))
			.Take(maximumPoints)
			.ToList();

		int remaining = maximumPoints - selected.Count;
		if (remaining > 0)
		{
			QualificationTelemetryPoint[] optional = candidates
				.Where(point => !protectedTicks.Contains(point.Elapsed.Ticks))
				.ToArray();
			double stride = optional.Length / (double)remaining;
			for (int index = 0; index < remaining; index++)
			{
				int candidateIndex = Math.Min(
					optional.Length - 1,
					(int)Math.Floor(index * stride));
				if (candidateIndex >= 0)
					selected.Add(optional[candidateIndex]);
			}
		}

		return selected
			.GroupBy(point => point.Elapsed.Ticks)
			.Select(group => group.Last())
			.OrderBy(point => point.Elapsed)
			.Take(maximumPoints)
			.ToArray();
	}

	private static TimeSpan CalculateInitialInterval(TimeSpan expectedDuration, int maximumPoints)
	{
		if (expectedDuration <= TimeSpan.Zero)
			return MinimumSamplingInterval;

		long targetTicks = expectedDuration.Ticks / Math.Max(1, maximumPoints - 2);
		return TimeSpan.FromTicks(Math.Max(MinimumSamplingInterval.Ticks, targetTicks));
	}

	private static double? Finite(double value) =>
		double.IsFinite(value) ? value : null;

	private sealed class ExtremePair
	{
		public double? Minimum { get; set; }
		public double? Maximum { get; set; }
		public QualificationTelemetryPoint? MinimumPoint { get; set; }
		public QualificationTelemetryPoint? MaximumPoint { get; set; }
	}
}
