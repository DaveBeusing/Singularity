// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Tests.Hardware;

public sealed class GpuDeviceIdentityTests
{
	[Fact]
	public void AdapterLuidIdentifierRoundTrips()
	{
		long luid = unchecked((long)0xFEDCBA9876543210UL);

		string identifier = GpuDeviceIdentity.FromAdapterLuid(luid);

		Assert.Equal("dxgi:luid:FEDCBA9876543210", identifier);
		Assert.True(GpuDeviceIdentity.TryGetAdapterLuid(identifier, out long parsed));
		Assert.Equal(luid, parsed);
	}

	[Theory]
	[InlineData("nvml:0")]
	[InlineData("nvml:unavailable")]
	public void NvmlFallbackIdentifiersAreTransient(string identifier)
	{
		Assert.True(GpuDeviceIdentity.IsTransientNvmlIdentifier(identifier));
	}

	[Theory]
	[InlineData("GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE")]
	[InlineData("dxgi:luid:0000000000000042")]
	public void CanonicalIdentifiersAreNotTransient(string identifier)
	{
		Assert.False(GpuDeviceIdentity.IsTransientNvmlIdentifier(identifier));
	}
}
