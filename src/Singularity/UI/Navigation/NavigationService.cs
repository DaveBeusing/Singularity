// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Navigation;

public sealed class NavigationService
{
	private readonly IReadOnlyList<WorkspaceDefinition> definitions;
	private readonly Dictionary<WorkspaceId, WorkspaceDefinition> definitionsById;

	public NavigationService(
		IEnumerable<WorkspaceDefinition> definitions,
		WorkspaceId defaultWorkspace = WorkspaceId.Overview)
	{
		ArgumentNullException.ThrowIfNull(definitions);

		this.definitions = definitions.ToArray();
		if (this.definitions.Count == 0)
			throw new ArgumentException("At least one workspace definition is required.", nameof(definitions));

		definitionsById = this.definitions.ToDictionary(definition => definition.Id);

		if (!definitionsById.TryGetValue(defaultWorkspace, out WorkspaceDefinition? initialDefinition))
			throw new ArgumentOutOfRangeException(nameof(defaultWorkspace), defaultWorkspace, "Default workspace is not defined.");

		ActiveWorkspace = defaultWorkspace;
		ActiveContextItem = initialDefinition.SidebarItems.FirstOrDefault();
	}

	public IReadOnlyList<WorkspaceDefinition> Definitions => definitions;
	public WorkspaceId ActiveWorkspace { get; private set; }
	public WorkspaceDefinition ActiveDefinition => definitionsById[ActiveWorkspace];
	public NavigationItem? ActiveContextItem { get; private set; }
	public WorkspaceSelection? Selection { get; private set; }

	public event Action<WorkspaceDefinition>? WorkspaceChanged;
	public event Action<NavigationItem?>? ContextItemChanged;
	public event Action<WorkspaceSelection?>? SelectionChanged;

	public bool Navigate(WorkspaceId workspace)
	{
		if (!definitionsById.TryGetValue(workspace, out WorkspaceDefinition? definition))
			return false;

		if (ActiveWorkspace == workspace)
			return true;

		ActiveWorkspace = workspace;
		ActiveContextItem = definition.SidebarItems.FirstOrDefault();
		Selection = null;

		WorkspaceChanged?.Invoke(definition);
		ContextItemChanged?.Invoke(ActiveContextItem);
		SelectionChanged?.Invoke(null);
		return true;
	}

	public bool SelectContextItem(string itemId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

		NavigationItem? item = ActiveDefinition.SidebarItems.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, itemId, StringComparison.Ordinal));

		if (item is null)
			return false;

		if (ActiveContextItem == item)
			return true;

		ActiveContextItem = item;
		ContextItemChanged?.Invoke(item);

		SetSelection(
			new WorkspaceSelection(
				ActiveWorkspace,
				"Navigation",
				item.Id,
				item.Label));

		return true;
	}

	public bool SetSelection(WorkspaceSelection? selection)
	{
		if (selection is not null && selection.Workspace != ActiveWorkspace)
			return false;

		if (Selection == selection)
			return true;

		Selection = selection;
		SelectionChanged?.Invoke(selection);
		return true;
	}
}
