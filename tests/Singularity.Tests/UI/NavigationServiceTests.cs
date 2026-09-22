// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Navigation;
using Xunit;

namespace Singularity.Tests.UI;

public sealed class NavigationServiceTests
{
	[Fact]
	public void DefaultsToOverviewAndFirstContextItem()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());

		Assert.Equal(WorkspaceId.Overview, navigation.ActiveWorkspace);
		Assert.Equal("summary", navigation.ActiveContextItem?.Id);
	}

	[Fact]
	public void NavigateSwitchesWorkspaceAndContext()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());

		bool changed = navigation.Navigate(WorkspaceId.Platform);

		Assert.True(changed);
		Assert.Equal(WorkspaceId.Platform, navigation.ActiveWorkspace);
		Assert.Equal("system", navigation.ActiveContextItem?.Id);
		Assert.Null(navigation.Selection);
	}

	[Fact]
	public void ContextSelectionUpdatesScopedSelection()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());
		navigation.Navigate(WorkspaceId.Results);

		bool changed = navigation.SelectContextItem("validation");

		Assert.True(changed);
		Assert.Equal("validation", navigation.ActiveContextItem?.Id);
		Assert.Equal(
			new WorkspaceSelection(
				WorkspaceId.Results,
				"Navigation",
				"validation",
				"Validation"),
			navigation.Selection);
	}

	[Fact]
	public void UnknownWorkspaceFailsSafely()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());

		bool changed = navigation.Navigate((WorkspaceId)999);

		Assert.False(changed);
		Assert.Equal(WorkspaceId.Overview, navigation.ActiveWorkspace);
	}

	[Fact]
	public void UnknownContextItemDoesNotChangeSelection()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());
		navigation.Navigate(WorkspaceId.Reports);

		bool changed = navigation.SelectContextItem("missing");

		Assert.False(changed);
		Assert.Equal("history", navigation.ActiveContextItem?.Id);
	}

	[Fact]
	public void SelectionFromAnotherWorkspaceIsRejected()
	{
		NavigationService navigation = new(WorkspaceCatalog.CreateDefault());

		bool changed = navigation.SetSelection(
			new WorkspaceSelection(
				WorkspaceId.Platform,
				"Hardware",
				"cpu",
				"CPU"));

		Assert.False(changed);
		Assert.Null(navigation.Selection);
	}
}
