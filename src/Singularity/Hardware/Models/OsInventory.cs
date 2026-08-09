// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Models;

public sealed class OsInventory
{
	public string Name { get; init; } = "Unknown";
	public string Version { get; init; } = "Unknown";
	public string Build { get; init; } = "Unknown";
	public string Architecture { get; init; } = "Unknown";
	public string ComputerName { get; init; } = "Unknown";
	public string UserName { get; init; } = "Unknown";
	public DateTime InstallDate { get; set; }
	public DateTime BootTime { get; init; }
	public string DisplayVersion => $"{Name} {Version}";
	public string BuildInfo => $"Build {Build}";
	public string PlatformInfo => $"{Architecture} | {ComputerName}";

}