// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Core.Reporting;

public sealed class GpuQualificationEvidence
{
	public string Identifier { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public ValidationStatus Result { get; init; } = ValidationStatus.Unknown;
	public string ValidationMessage { get; init; } = string.Empty;
	public bool TelemetryAvailable { get; init; }
	public GpuTelemetryStatistics TelemetryStatistics { get; init; } = new();
}
