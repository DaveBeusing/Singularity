// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class CpuInfo
{
	public string Name { get; set; } = "Unknown";
	public string CoreThreadInfo { get; set; } = "Unknown";
	public string ClockInfo { get; set; } = "Unknown";
	public string CacheInfo { get; set; } = "Unknown";
	public string PlatformInfo { get; set; } = "Unknown";
	public string Details => $"{CoreThreadInfo} | {ClockInfo} | {CacheInfo} | {PlatformInfo}";
}