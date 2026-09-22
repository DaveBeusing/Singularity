// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Navigation;

public static class WorkspaceCatalog
{
	public static IReadOnlyList<WorkspaceDefinition> CreateDefault()
	{
		return
		[
			new(
				WorkspaceId.Overview,
				"Overview",
				"O",
				"Platform qualification summary and current system state.",
				[
					new("summary", "Summary"),
					new("status", "Status")
				],
				SupportsInspector: false,
				SupportsToolPanel: false),
			new(
				WorkspaceId.Platform,
				"Platform",
				"P",
				"Hardware inventory and platform details.",
				[
					new("system", "System"),
					new("cpu", "CPU"),
					new("memory", "Memory"),
					new("gpu", "GPU"),
					new("storage", "Storage")
				],
				SupportsInspector: true,
				SupportsToolPanel: false),
			new(
				WorkspaceId.Qualification,
				"Qualification",
				"Q",
				"Qualification profile, workloads, live telemetry, and run controls.",
				[
					new("profile", "Profile"),
					new("workloads", "Workloads"),
					new("session", "Session")
				],
				SupportsInspector: true,
				SupportsToolPanel: true),
			new(
				WorkspaceId.Results,
				"Results",
				"R",
				"Latest qualification result, validation evidence, and statistics.",
				[
					new("latest", "Latest"),
					new("validation", "Validation"),
					new("statistics", "Statistics")
				],
				SupportsInspector: true,
				SupportsToolPanel: true),
			new(
				WorkspaceId.Reports,
				"Reports",
				"D",
				"Qualification history, report preview, and export.",
				[
					new("history", "History"),
					new("preview", "Preview"),
					new("export", "Export")
				],
				SupportsInspector: true,
				SupportsToolPanel: false),
			new(
				WorkspaceId.Settings,
				"Settings",
				"S",
				"Application settings and presentation preferences.",
				[
					new("general", "General"),
					new("appearance", "Appearance")
				],
				SupportsInspector: false,
				SupportsToolPanel: false)
		];
	}
}
