// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class CpuInventory
{
	public string Name { get; init; } = "Unknown";
	public int CoreCount { get; init; }
	public int ThreadCount { get; init; }
	public int MaxClockMhz { get; init; }
	public int L2CacheKb { get; init; }
	public int L3CacheKb { get; init; }
	public string Socket { get; init; } = "Unknown";
	public bool VirtualizationEnabled { get; init; }
	public string CoreThreadInfo => $"{CoreCount} Cores / {ThreadCount} Threads";
	public string ClockInfo => $"{MaxClockMhz / 1000d:0.00} GHz";
	public string CacheInfo => $"L2 {L2CacheKb / 1024d:0.#} MB | L3 {L3CacheKb / 1024d:0.#} MB";
	public string PlatformInfo => $"{Socket} | VTx {(VirtualizationEnabled ? "Enabled" : "Disabled")}";

}