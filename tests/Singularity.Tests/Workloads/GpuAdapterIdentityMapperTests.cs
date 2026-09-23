// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;

namespace Singularity.Tests.Workloads;

public sealed class GpuAdapterIdentityMapperTests
{
	[Fact]
	public void ResolveLuid_MapsStableIdentifierIndependentOfEnumerationOrder()
	{
		GpuAdapterIdentity first = new(
			"GPU-11111111-2222-3333-4444-555555555555",
			101);
		GpuAdapterIdentity selected = new(
			"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
			202);

		long luid = GpuAdapterIdentityMapper.ResolveLuid(
			[selected, first],
			"gpu-aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

		Assert.Equal(202, luid);
	}

	[Fact]
	public void ResolveLuid_RejectsUnknownIdentifier()
	{
		GpuAdapterIdentity adapter = new(
			"GPU-11111111-2222-3333-4444-555555555555",
			101);

		InvalidOperationException error = Assert.Throws<InvalidOperationException>(
			() => GpuAdapterIdentityMapper.ResolveLuid(
				[adapter],
				"GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"));

		Assert.Contains("could not be resolved", error.Message, StringComparison.OrdinalIgnoreCase);
	}
}
