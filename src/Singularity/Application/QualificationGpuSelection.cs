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

	public static QualificationGpuOption? ResolveSelection(
		IReadOnlyList<QualificationGpuOption> options,
		string? selectedIdentifier)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (string.IsNullOrWhiteSpace(selectedIdentifier))
			return options.Count > 0 ? options[0] : null;

		return options.FirstOrDefault(
			option => string.Equals(
				option.Identifier,
				selectedIdentifier,
				StringComparison.OrdinalIgnoreCase));
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
