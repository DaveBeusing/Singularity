// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

public sealed class OsInfo
{
	public string Name { get; set; } = "Unknown";
	public string Version { get; set; } = "Unknown";
	public string Architecture { get; set; } = "Unknown";
	public string BuildNumber { get; set; } = "Unknown";
	public string InstallDate { get; set; } = "Unknown";
	public string LastBootTime { get; set; } = "Unknown";
	public string User { get; set; } = "Unknown";
}