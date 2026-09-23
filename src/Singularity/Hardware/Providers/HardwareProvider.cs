// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Hardware.Providers;

public sealed class HardwareProvider
{
	private readonly MainboardProvider mainboardProvider = new();
	private readonly CpuProvider cpuProvider = new();
	private readonly MemoryProvider memoryProvider = new();
	private readonly OsProvider osProvider = new();
	private readonly WindowsGpuProvider windowsGpuProvider = new();
	private readonly NvmlGpuProvider nvmlGpuProvider = new();
	private readonly StorageProvider storageProvider = new();

	public HardwareInventory Read()
	{
		IReadOnlyList<NvidiaGpuAdapterIdentity> nvidiaIdentities =
			NvidiaGpuAdapterIdentityProvider.ReadAll();
		IReadOnlyList<GpuInventory> baselineGpus =
			windowsGpuProvider.ReadAll(nvidiaIdentities);
		IReadOnlyList<GpuInventory> enrichmentGpus =
			nvmlGpuProvider.ReadAll(nvidiaIdentities);

		return new HardwareInventory
		{
			Mainboard = mainboardProvider.Read(),
			Cpu = cpuProvider.Read(),
			MemoryModules = memoryProvider.Read(),
			Os = osProvider.Read(),
			Gpus = GpuInventoryMerger.Merge(baselineGpus, enrichmentGpus),
			StorageDrives = storageProvider.Read()
		};
	}
}
