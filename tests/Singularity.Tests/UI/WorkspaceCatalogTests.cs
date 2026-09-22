// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Navigation;
using Xunit;

namespace Singularity.Tests.UI;

public sealed class WorkspaceCatalogTests
{
	[Fact]
	public void DefaultCatalogContainsCanonicalWorkspacesInOrder()
	{
		IReadOnlyList<WorkspaceDefinition> workspaces = WorkspaceCatalog.CreateDefault();

		WorkspaceId[] expected =
		[
			WorkspaceId.Overview,
			WorkspaceId.Platform,
			WorkspaceId.Qualification,
			WorkspaceId.Results,
			WorkspaceId.Reports,
			WorkspaceId.Settings
		];

		Assert.Equal(
			expected,
			workspaces.Select(workspace => workspace.Id).ToArray());
	}

	[Theory]
	[InlineData(WorkspaceId.Platform, "system")]
	[InlineData(WorkspaceId.Qualification, "profile")]
	[InlineData(WorkspaceId.Results, "latest")]
	[InlineData(WorkspaceId.Reports, "history")]
	public void WorkspaceProvidesExpectedInitialContext(
		WorkspaceId workspaceId,
		string contextId)
	{
		WorkspaceDefinition workspace = WorkspaceCatalog.CreateDefault()
			.Single(candidate => candidate.Id == workspaceId);

		Assert.Equal(contextId, workspace.SidebarItems[0].Id);
	}
}
