// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class HardwareInventory
{
	public MainboardInventory Mainboard { get; init; } = new();
	public CpuInventory Cpu { get; init; } = new();
	public GpuInventory Gpu { get; init; } = new();
	public OsInventory Os { get; init; } = new();
	public IReadOnlyList<MemoryInventory> MemoryModules { get; init; } = Array.Empty<MemoryInventory>();
	public IReadOnlyList<StorageInventory> StorageDrives { get; init; } = Array.Empty<StorageInventory>();

}