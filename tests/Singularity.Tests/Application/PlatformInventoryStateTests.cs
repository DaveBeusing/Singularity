// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Hardware.Models;
using Xunit;

namespace Singularity.Tests.Application;

public sealed class PlatformInventoryStateTests
{
	[Fact]
	public async Task RefreshCachesInventoryAndMarksStateAvailable()
	{
		HardwareInventory expected = CreateInventory();
		int reads = 0;
		PlatformInventoryState state = new(() =>
		{
			reads++;
			return expected;
		});

		bool refreshed = await state.RefreshAsync();

		Assert.True(refreshed);
		Assert.Same(expected, state.Current);
		Assert.Equal(1, reads);
		Assert.Equal(PlatformInventoryStatus.Available, state.Status);
		Assert.NotNull(state.LastUpdatedAtUtc);
		Assert.Null(state.LastError);
	}

	[Fact]
	public async Task FailedRefreshPreservesLastValidInventory()
	{
		HardwareInventory expected = CreateInventory();
		int reads = 0;
		PlatformInventoryState state = new(() =>
		{
			reads++;
			if (reads == 1)
				return expected;

			throw new InvalidOperationException("inventory failed");
		});

		Assert.True(await state.RefreshAsync());
		bool refreshed = await state.RefreshAsync();

		Assert.False(refreshed);
		Assert.Same(expected, state.Current);
		Assert.Equal(PlatformInventoryStatus.Failed, state.Status);
		Assert.Equal("inventory failed", state.LastError);
	}

	[Fact]
	public async Task ConcurrentRefreshIsRejected()
	{
		using ManualResetEventSlim entered = new(false);
		using ManualResetEventSlim release = new(false);
		PlatformInventoryState state = new(() =>
		{
			entered.Set();
			Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
			return CreateInventory();
		});

		Task<bool> firstRefresh = state.RefreshAsync();
		Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));

		bool secondRefresh = await state.RefreshAsync();
		release.Set();

		Assert.False(secondRefresh);
		Assert.True(await firstRefresh);
		Assert.Equal(PlatformInventoryStatus.Available, state.Status);
	}

	private static HardwareInventory CreateInventory()
	{
		return new HardwareInventory
		{
			Cpu = new CpuInventory { Name = "Test CPU" },
			Gpus = [new GpuInventory { Name = "Test GPU" }],
			MemoryModules = [new MemoryInventory { Slot = "DIMM_A1" }],
			StorageDrives = [new StorageInventory { Model = "Test SSD" }]
		};
	}
}
