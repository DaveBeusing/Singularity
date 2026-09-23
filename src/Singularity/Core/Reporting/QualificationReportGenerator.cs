// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.Core.Reporting;

public sealed class QualificationReportGenerator
{
	public QualificationReport Create(
		QualificationSession session,
		ValidationResult validationResult)
	{
		if (session.StartTime is null)
			throw new InvalidOperationException("Session has no start time.");

		if (session.EndTime is null)
			throw new InvalidOperationException("Session has no end time.");

		return new QualificationReport
		{
			StartedAt = session.StartTime.Value,
			FinishedAt = session.EndTime.Value,
			Duration = session.Duration,
			Profile = session.Profile,
			ExecutionMode = session.ExecutionMode,
			CpuResult = validationResult.CpuStatus,
			MemoryResult = validationResult.MemoryStatus,
			GpuResult = validationResult.GpuStatus,
			OverallResult = session.Result,
			TelemetryStatistics = session.TelemetryStatistics,
			TelemetryTimeline = session.TelemetryTimeline,
			GpuEvidence = BuildGpuEvidence(session, validationResult)
		};
	}

	private static IReadOnlyList<GpuQualificationEvidence> BuildGpuEvidence(
		QualificationSession session,
		ValidationResult validation)
	{
		if (session.SelectedGpuIdentifiers.Count == 0 &&
			validation.GpuDevices.Count == 0)
		{
			return Array.Empty<GpuQualificationEvidence>();
		}

		Dictionary<string, GpuTelemetryStatistics> statistics =
			session.TelemetryStatistics.Gpus.ToDictionary(
				item => item.Identifier,
				StringComparer.OrdinalIgnoreCase);
		Dictionary<string, GpuValidationResult> validationByDevice =
			validation.GpuDevices.ToDictionary(
				item => item.Identifier,
				StringComparer.OrdinalIgnoreCase);

		List<string> identifiers = [.. session.SelectedGpuIdentifiers];
		foreach (GpuValidationResult result in validation.GpuDevices)
		{
			if (!identifiers.Contains(result.Identifier, StringComparer.OrdinalIgnoreCase))
				identifiers.Add(result.Identifier);
		}

		GpuQualificationEvidence[] evidence =
			new GpuQualificationEvidence[identifiers.Count];
		for (int index = 0; index < identifiers.Count; index++)
		{
			string identifier = identifiers[index];
			statistics.TryGetValue(identifier, out GpuTelemetryStatistics? telemetry);
			validationByDevice.TryGetValue(identifier, out GpuValidationResult? result);

			GpuTelemetryStatistics frozenTelemetry = telemetry ??
				new GpuTelemetryStatistics { Identifier = identifier };
			string validationName = result?.Name ?? string.Empty;
			string evidenceName = !string.IsNullOrWhiteSpace(validationName)
				? validationName
				: !string.IsNullOrWhiteSpace(frozenTelemetry.Name)
					? frozenTelemetry.Name
					: identifier;

			evidence[index] = new GpuQualificationEvidence
			{
				Identifier = identifier,
				Name = evidenceName,
				Result = result?.Status ?? ValidationStatus.Unknown,
				ValidationMessage = result?.Message ?? "Validation unavailable",
				TelemetryAvailable = result?.TelemetryAvailable ??
					frozenTelemetry.TelemetryAvailable,
				TelemetryStatistics = frozenTelemetry
			};
		}

		return Array.AsReadOnly(evidence);
	}
}
