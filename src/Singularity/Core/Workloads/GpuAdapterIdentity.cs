// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed record GpuAdapterIdentity(string Identifier, long AdapterLuid);

public static class GpuAdapterIdentityMapper
{
	public static long ResolveLuid(
		IReadOnlyList<GpuAdapterIdentity> adapters,
		string selectedIdentifier)
	{
		ArgumentNullException.ThrowIfNull(adapters);
		ArgumentException.ThrowIfNullOrWhiteSpace(selectedIdentifier);

		string normalizedSelection = NormalizeIdentifier(selectedIdentifier);
		GpuAdapterIdentity? adapter = adapters.FirstOrDefault(
			candidate => string.Equals(
				NormalizeIdentifier(candidate.Identifier),
				normalizedSelection,
				StringComparison.Ordinal));

		return adapter?.AdapterLuid
			?? throw new InvalidOperationException(
				$"Selected GPU '{selectedIdentifier}' could not be resolved to a Windows graphics adapter.");
	}

	public static string NormalizeIdentifier(string identifier)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
		return new string(identifier.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
	}
}
