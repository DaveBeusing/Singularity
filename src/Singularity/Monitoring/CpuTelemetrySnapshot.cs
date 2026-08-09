// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class CpuTelemetrySnapshot
{
	public bool IsAvailable { get; init; }
	public double TemperatureCelsius { get; init; }
	public string Status { get; init; } = "Unavailable";

}