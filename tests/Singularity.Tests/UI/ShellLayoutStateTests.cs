// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Shell;
using Xunit;

namespace Singularity.Tests.UI;

public sealed class ShellLayoutStateTests
{
	[Fact]
	public void DefaultStateKeepsContextNavigationVisible()
	{
		ShellLayoutState state = ShellLayoutState.Default;

		Assert.True(state.SidebarVisible);
		Assert.False(state.InspectorVisible);
		Assert.False(state.ToolPanelVisible);
	}

	[Fact]
	public void RegionChangesPreserveUnrelatedVisibility()
	{
		ShellLayoutState initial = ShellLayoutState.Default;

		ShellLayoutState changed = initial
			.WithInspector(true)
			.WithToolPanel(true)
			.WithSidebar(false);

		Assert.False(changed.SidebarVisible);
		Assert.True(changed.InspectorVisible);
		Assert.True(changed.ToolPanelVisible);

		Assert.True(initial.SidebarVisible);
		Assert.False(initial.InspectorVisible);
		Assert.False(initial.ToolPanelVisible);
	}
}
