// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class GpuTelemetrySnapshot
{
	public bool IsAvailable { get; init; }
	public double LoadPercent { get; init; }
	public double MemoryLoadPercent { get; init; }
	public int TemperatureCelsius { get; init; }
	public string Status { get; init; } = "Unavailable";

}