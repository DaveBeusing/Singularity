// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Application;

public sealed record QualificationGpuOption(
	string Identifier,
	string DisplayName);

public static class QualificationGpuSelection
{
	public static IReadOnlyList<QualificationGpuOption> CreateOptions(
		IReadOnlyList<GpuInventory> gpus)
	{
		ArgumentNullException.ThrowIfNull(gpus);

		List<QualificationGpuOption> options = [];
		HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);

		foreach (GpuInventory gpu in gpus)
		{
			if (!IsStableIdentifier(gpu.Identifier) ||
				!identifiers.Add(gpu.Identifier))
			{
				continue;
			}

			options.Add(new QualificationGpuOption(
				gpu.Identifier,
				CreateDisplayName(gpu)));
		}

		return options.AsReadOnly();
	}

	public static IReadOnlyList<QualificationGpuOption> ResolveSelections(
		IReadOnlyList<QualificationGpuOption> options,
		IReadOnlyList<string> selectedIdentifiers)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(selectedIdentifiers);

		if (selectedIdentifiers.Count == 0)
		{
			return options.Count == 1
				? [options[0]]
				: Array.Empty<QualificationGpuOption>();
		}

		List<QualificationGpuOption> selections = [];
		HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);

		foreach (string identifier in selectedIdentifiers)
		{
			if (string.IsNullOrWhiteSpace(identifier) || !added.Add(identifier))
				continue;

			QualificationGpuOption? option = options.FirstOrDefault(
				candidate => string.Equals(
					candidate.Identifier,
					identifier,
					StringComparison.OrdinalIgnoreCase));
			if (option is not null)
				selections.Add(option);
		}

		return selections.AsReadOnly();
	}

	public static QualificationGpuOption? ResolveSelection(
		IReadOnlyList<QualificationGpuOption> options,
		string? selectedIdentifier)
	{
		IReadOnlyList<QualificationGpuOption> selections = ResolveSelections(
			options,
			string.IsNullOrWhiteSpace(selectedIdentifier)
				? Array.Empty<string>()
				: [selectedIdentifier]);

		return selections.Count > 0 ? selections[0] : null;
	}

	public static bool IsStableIdentifier(string? identifier)
	{
		return !string.IsNullOrWhiteSpace(identifier) &&
			!identifier.StartsWith("nvml:", StringComparison.OrdinalIgnoreCase);
	}

	private static string CreateDisplayName(GpuInventory gpu)
	{
		string name = string.IsNullOrWhiteSpace(gpu.Name) ? "GPU" : gpu.Name;
		string identifier = gpu.Identifier;
		string suffix = identifier.Length > 12 ? identifier[^12..] : identifier;
		return $"{name} · {suffix}";
	}
}
