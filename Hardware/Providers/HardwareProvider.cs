// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;

namespace Singularity.Hardware.Providers;

public sealed class HardwareProvider
{
	private readonly MainboardProvider mainboardProvider = new();
	private readonly CpuProvider cpuProvider = new();
	private readonly MemoryProvider memoryProvider = new();
	private readonly OsProvider osProvider = new();
	private readonly NvmlGpuProvider gpuProvider = new();
	public HardwareInventory Read()
	{
		return new HardwareInventory
		{
			Mainboard = mainboardProvider.Read(),
			Cpu = cpuProvider.Read(),
			MemoryModules = memoryProvider.Read(),
			Os = osProvider.Read(),
			Gpu = gpuProvider.Read()
		};
	}

}