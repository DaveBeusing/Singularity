// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;

namespace Singularity.Core.Workloads;

internal static class NvidiaGpuAdapterResolver
{
	public static long ResolveAdapterLuid(string selectedIdentifier)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(selectedIdentifier);

		if (GpuDeviceIdentity.TryGetAdapterLuid(
			selectedIdentifier,
			out long adapterLuid))
		{
			return adapterLuid;
		}

		IReadOnlyList<NvidiaGpuAdapterIdentity> identities =
			NvidiaGpuAdapterIdentityProvider.ReadAll();
		if (identities.Count == 0)
		{
			throw new InvalidOperationException(
				"The selected GPU identity could not be resolved to a Windows graphics adapter.");
		}

		GpuAdapterIdentity[] adapters = identities
			.Select(identity => new GpuAdapterIdentity(
				identity.Identifier,
				identity.AdapterLuid))
			.ToArray();

		return GpuAdapterIdentityMapper.ResolveLuid(
			adapters,
			selectedIdentifier);
	}
}
