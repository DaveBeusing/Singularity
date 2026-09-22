// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Navigation;

public sealed record WorkspaceDefinition(
	WorkspaceId Id,
	string Title,
	string ActivityText,
	string Description,
	IReadOnlyList<NavigationItem> SidebarItems,
	bool SupportsInspector,
	bool SupportsToolPanel);
