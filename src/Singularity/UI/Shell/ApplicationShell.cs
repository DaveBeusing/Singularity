// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Commands;
using Singularity.UI.Controls;
using Singularity.UI.Navigation;

namespace Singularity.UI.Shell;

public sealed class ApplicationShell : UserControl
{
	private readonly NavigationService navigationService;
	private readonly CommandRouter commandRouter;
	private readonly ActivityBar activityBar;
	private readonly SidebarHost sidebarHost = new();
	private readonly WorkspaceHost workspaceHost = new();
	private readonly InspectorHost inspectorHost = new();
	private readonly ToolPanelHost toolPanelHost = new();
	private readonly ApplicationStatusBar statusBar = new();
	private readonly Dictionary<WorkspaceId, Control> inspectorContent = [];
	private readonly Dictionary<WorkspaceId, Control> toolPanelContent = [];

	private readonly SplitContainer bodySplit = new();
	private readonly SplitContainer inspectorSplit = new();
	private readonly SplitContainer toolSplit = new();
	private readonly Label workspaceTitleLabel = new();

	private int savedSidebarWidth = ThemeMetrics.DefaultSidebarWidth;
	private bool applyingLayoutState;

	public ApplicationShell(
		string version,
		NavigationService navigationService,
		CommandRouter commandRouter)
	{
		this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		this.commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
		RegisterShellCommands();
		activityBar = new ActivityBar(navigationService.Definitions, commandRouter);

		AutoScaleMode = AutoScaleMode.Inherit;
		BackColor = Theme.ApplicationBackground;
		Size = new Size(ThemeMetrics.DefaultWindowWidth, ThemeMetrics.DefaultWindowHeight);

		BuildLayout(version);
		WireInteractions();

		LayoutState = ShellLayoutState.Default;
		PerformLayout();
		ApplyLayoutState();
		ApplyNavigationState(navigationService.ActiveDefinition);
	}

	public ShellLayoutState LayoutState { get; private set; }
	public WorkspaceHost Workspace => workspaceHost;
	public SidebarHost Sidebar => sidebarHost;
	public InspectorHost Inspector => inspectorHost;
	public ToolPanelHost ToolPanel => toolPanelHost;

	public event Action<ShellLayoutState>? LayoutStateChanged;

	public void RegisterWorkspace(WorkspaceId workspace, Control content)
	{
		workspaceHost.Register(workspace, content);

		if (workspace == navigationService.ActiveWorkspace)
			workspaceHost.TryActivate(workspace);
	}

	public void RegisterInspectorContent(WorkspaceId workspace, Control content)
	{
		ArgumentNullException.ThrowIfNull(content);
		inspectorContent.Add(workspace, content);

		if (workspace == navigationService.ActiveWorkspace)
			inspectorHost.SetContent(content);
	}

	public void RegisterToolPanelContent(WorkspaceId workspace, Control content)
	{
		ArgumentNullException.ThrowIfNull(content);
		toolPanelContent.Add(workspace, content);

		if (workspace == navigationService.ActiveWorkspace)
			toolPanelHost.SetContent(content);
	}

	public void SetGlobalStatus(string text, StatusVisualState state)
	{
		statusBar.SetStatus(text, state);
	}

	public void SetStatusDetails(string details)
	{
		statusBar.SetDetails(details);
	}

	public void SetSidebarVisible(bool visible)
	{
		if (LayoutState.SidebarVisible == visible)
			return;

		if (!visible)
			savedSidebarWidth = Math.Max(
				0,
				bodySplit.SplitterDistance - ThemeMetrics.ActivityBarWidth);

		LayoutState = LayoutState.WithSidebar(visible);
		ApplyLayoutState();
		LayoutStateChanged?.Invoke(LayoutState);
		commandRouter.RefreshStates();
	}

	public void SetInspectorVisible(bool visible)
	{
		if (LayoutState.InspectorVisible == visible)
			return;

		LayoutState = LayoutState.WithInspector(visible);
		ApplyLayoutState();
		LayoutStateChanged?.Invoke(LayoutState);
		commandRouter.RefreshStates();
	}

	public void SetToolPanelVisible(bool visible)
	{
		if (LayoutState.ToolPanelVisible == visible)
			return;

		LayoutState = LayoutState.WithToolPanel(visible);
		ApplyLayoutState();
		LayoutStateChanged?.Invoke(LayoutState);
		commandRouter.RefreshStates();
	}

	public void ResetLayout()
	{
		savedSidebarWidth = ThemeMetrics.DefaultSidebarWidth;
		LayoutState = ShellLayoutState.Default;
		ApplyLayoutState();
		LayoutStateChanged?.Invoke(LayoutState);
		commandRouter.RefreshStates();
	}

	public bool HandleShortcut(Keys keyData)
	{
		if ((keyData & Keys.Control) != Keys.Control)
			return false;

		return keyData switch
		{
			Keys.Control | Keys.D1 => navigationService.Navigate(WorkspaceId.Overview),
			Keys.Control | Keys.D2 => navigationService.Navigate(WorkspaceId.Platform),
			Keys.Control | Keys.D3 => navigationService.Navigate(WorkspaceId.Qualification),
			Keys.Control | Keys.D4 => navigationService.Navigate(WorkspaceId.Results),
			Keys.Control | Keys.D5 => navigationService.Navigate(WorkspaceId.Reports),
			Keys.Control | Keys.D6 => navigationService.Navigate(WorkspaceId.Settings),
			Keys.Control | Keys.B => commandRouter.Execute(CommandId.ToggleSidebar),
			Keys.Control | Keys.Alt | Keys.I => commandRouter.Execute(CommandId.ToggleInspector),
			Keys.Control | Keys.J => commandRouter.Execute(CommandId.ToggleToolPanel),
			Keys.Control | Keys.R when navigationService.ActiveWorkspace == WorkspaceId.Platform =>
				commandRouter.Execute(CommandId.RefreshInventory),
			_ => false
		};
	}

	private void RegisterShellCommands()
	{
		if (!commandRouter.IsRegistered(CommandId.ToggleSidebar))
		{
			commandRouter.Register(
				CommandId.ToggleSidebar,
				() => SetSidebarVisible(!LayoutState.SidebarVisible));
		}

		if (!commandRouter.IsRegistered(CommandId.ToggleInspector))
		{
			commandRouter.Register(
				CommandId.ToggleInspector,
				() => SetInspectorVisible(!LayoutState.InspectorVisible),
				() => navigationService.ActiveDefinition.SupportsInspector);
		}

		if (!commandRouter.IsRegistered(CommandId.ToggleToolPanel))
		{
			commandRouter.Register(
				CommandId.ToggleToolPanel,
				() => SetToolPanelVisible(!LayoutState.ToolPanelVisible),
				() => navigationService.ActiveDefinition.SupportsToolPanel);
		}
	}

	private void BuildLayout(string version)
	{
		Panel header = BuildHeader(version);

		bodySplit.Size = new Size(
			ThemeMetrics.DefaultWindowWidth,
			ThemeMetrics.DefaultWindowHeight - ThemeMetrics.HeaderHeight - ThemeMetrics.StatusBarHeight);
		bodySplit.Dock = DockStyle.Fill;
		bodySplit.Orientation = Orientation.Vertical;
		bodySplit.BorderStyle = BorderStyle.None;
		bodySplit.SplitterWidth = ThemeMetrics.SplitterWidth;
		bodySplit.BackColor = Theme.Separator;
		bodySplit.FixedPanel = FixedPanel.Panel1;
		bodySplit.SplitterDistance = ThemeMetrics.ActivityBarWidth + ThemeMetrics.DefaultSidebarWidth;
		bodySplit.Panel1MinSize = ThemeMetrics.ActivityBarWidth;
		bodySplit.Panel2MinSize = ThemeMetrics.MinimumWorkspaceWidth;

		Panel navigationRegion = new()
		{
			Dock = DockStyle.Fill,
			BackColor = Theme.Sidebar
		};
		sidebarHost.Dock = DockStyle.Fill;
		activityBar.Dock = DockStyle.Left;
		navigationRegion.Controls.Add(sidebarHost);
		navigationRegion.Controls.Add(activityBar);
		bodySplit.Panel1.Controls.Add(navigationRegion);

		inspectorSplit.Size = new Size(
			ThemeMetrics.DefaultWindowWidth - ThemeMetrics.ActivityBarWidth - ThemeMetrics.DefaultSidebarWidth,
			bodySplit.Height);
		inspectorSplit.Dock = DockStyle.Fill;
		inspectorSplit.Orientation = Orientation.Vertical;
		inspectorSplit.BorderStyle = BorderStyle.None;
		inspectorSplit.SplitterWidth = ThemeMetrics.SplitterWidth;
		inspectorSplit.BackColor = Theme.Separator;
		inspectorSplit.FixedPanel = FixedPanel.Panel2;
		inspectorSplit.SplitterDistance = inspectorSplit.Width - ThemeMetrics.InspectorWidth - inspectorSplit.SplitterWidth;
		inspectorSplit.Panel1MinSize = ThemeMetrics.MinimumWorkspaceWidth;
		inspectorSplit.Panel2MinSize = 180;
		inspectorHost.Dock = DockStyle.Fill;
		inspectorSplit.Panel2.Controls.Add(inspectorHost);

		toolSplit.Size = inspectorSplit.Size;
		toolSplit.Dock = DockStyle.Fill;
		toolSplit.Orientation = Orientation.Horizontal;
		toolSplit.BorderStyle = BorderStyle.None;
		toolSplit.SplitterWidth = ThemeMetrics.SplitterWidth;
		toolSplit.BackColor = Theme.Separator;
		toolSplit.FixedPanel = FixedPanel.Panel2;
		toolSplit.SplitterDistance = toolSplit.Height - ThemeMetrics.ToolPanelHeight - toolSplit.SplitterWidth;
		toolSplit.Panel1MinSize = ThemeMetrics.MinimumWorkspaceHeight;
		toolSplit.Panel2MinSize = 100;
		workspaceHost.Dock = DockStyle.Fill;
		toolPanelHost.Dock = DockStyle.Fill;
		toolSplit.Panel1.Controls.Add(workspaceHost);
		toolSplit.Panel2.Controls.Add(toolPanelHost);

		inspectorSplit.Panel1.Controls.Add(toolSplit);
		bodySplit.Panel2.Controls.Add(inspectorSplit);

		inspectorSplit.Panel2Collapsed = true;
		toolSplit.Panel2Collapsed = true;

		Controls.Add(bodySplit);
		Controls.Add(statusBar);
		Controls.Add(header);
	}

	private Panel BuildHeader(string version)
	{
		Panel header = new()
		{
			Dock = DockStyle.Top,
			Height = ThemeMetrics.HeaderHeight,
			BackColor = Theme.ApplicationBackground,
			Padding = new Padding(ThemeMetrics.HeaderHorizontalPadding, 0, ThemeMetrics.HeaderHorizontalPadding, 0)
		};

		Label title = new()
		{
			Dock = DockStyle.Left,
			Width = 245,
			Text = "//Singularity✦",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.ApplicationBackground,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label versionLabel = new()
		{
			Dock = DockStyle.Right,
			Width = 130,
			Text = version,
			Font = ThemeFonts.SectionHeader,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.ApplicationBackground,
			TextAlign = ContentAlignment.MiddleRight
		};

		workspaceTitleLabel.Dock = DockStyle.Fill;
		workspaceTitleLabel.Font = ThemeFonts.Subtitle;
		workspaceTitleLabel.ForeColor = Theme.TextMuted;
		workspaceTitleLabel.BackColor = Theme.ApplicationBackground;
		workspaceTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

		SectionSeparator separator = new()
		{
			Dock = DockStyle.Bottom
		};

		header.Controls.Add(workspaceTitleLabel);
		header.Controls.Add(versionLabel);
		header.Controls.Add(title);
		header.Controls.Add(separator);
		return header;
	}

	private void WireInteractions()
	{
		activityBar.NavigationRequested += workspace => navigationService.Navigate(workspace);
		navigationService.WorkspaceChanged += ApplyNavigationState;
		navigationService.ContextItemChanged += _ => RefreshSidebar();
		navigationService.SelectionChanged += inspectorHost.SetSelection;

		bodySplit.SplitterMoved += (_, _) =>
		{
			if (!applyingLayoutState && LayoutState.SidebarVisible)
			{
				savedSidebarWidth = Math.Max(
					0,
					bodySplit.SplitterDistance - ThemeMetrics.ActivityBarWidth);
			}
		};
	}

	private void ApplyNavigationState(WorkspaceDefinition workspace)
	{
		activityBar.SetActive(workspace.Id);
		workspaceTitleLabel.Text = workspace.Title;
		statusBar.SetContext(workspace.Title);
		RefreshSidebar();
		workspaceHost.TryActivate(workspace.Id);

		inspectorHost.SetContent(
			inspectorContent.TryGetValue(workspace.Id, out Control? inspector) ? inspector : null);
		toolPanelHost.SetContent(
			toolPanelContent.TryGetValue(workspace.Id, out Control? tools) ? tools : null);

		ApplyLayoutState();
		commandRouter.RefreshStates();
	}

	private void RefreshSidebar()
	{
		sidebarHost.SetContext(
			navigationService.ActiveDefinition,
			navigationService.ActiveContextItem,
			item => navigationService.SelectContextItem(item.Id));
	}

	private void ApplyLayoutState()
	{
		if (bodySplit.ClientSize.Width <= 0 || bodySplit.ClientSize.Height <= 0)
			return;

		applyingLayoutState = true;
		try
		{
			bool inspectorVisible =
				LayoutState.InspectorVisible &&
				navigationService.ActiveDefinition.SupportsInspector;
			bool toolPanelVisible =
				LayoutState.ToolPanelVisible &&
				navigationService.ActiveDefinition.SupportsToolPanel;

			sidebarHost.Visible = LayoutState.SidebarVisible;

			int desiredNavigationWidth = ThemeMetrics.ActivityBarWidth;
			if (LayoutState.SidebarVisible)
				desiredNavigationWidth += savedSidebarWidth;

			int requiredContentWidth = ThemeMetrics.MinimumWorkspaceWidth;
			if (inspectorVisible)
			{
				requiredContentWidth += inspectorSplit.SplitterWidth + inspectorSplit.Panel2MinSize;
			}
			else
			{
				bodySplit.Panel2MinSize = ThemeMetrics.MinimumWorkspaceWidth;
			}

			int maximumNavigationWidth = Math.Max(
				ThemeMetrics.ActivityBarWidth,
				bodySplit.ClientSize.Width - bodySplit.SplitterWidth - requiredContentWidth);
			bodySplit.SplitterDistance = Math.Min(desiredNavigationWidth, maximumNavigationWidth);

			if (inspectorVisible)
				bodySplit.Panel2MinSize = requiredContentWidth;

			bodySplit.PerformLayout();

			inspectorSplit.Panel2Collapsed = !inspectorVisible;
			if (inspectorVisible)
				ApplyInspectorWidth();

			toolSplit.Panel2Collapsed = !toolPanelVisible;
			if (toolPanelVisible)
				ApplyToolPanelHeight();
		}
		finally
		{
			applyingLayoutState = false;
		}
	}

	private void ApplyInspectorWidth()
	{
		int availableWidth = inspectorSplit.ClientSize.Width - inspectorSplit.SplitterWidth;
		if (availableWidth <= inspectorSplit.Panel1MinSize)
			return;

		int inspectorWidth = Math.Min(
			ThemeMetrics.InspectorWidth,
			availableWidth - inspectorSplit.Panel1MinSize);
		inspectorSplit.SplitterDistance = availableWidth - Math.Max(inspectorSplit.Panel2MinSize, inspectorWidth);
	}

	private void ApplyToolPanelHeight()
	{
		int availableHeight = toolSplit.ClientSize.Height - toolSplit.SplitterWidth;
		if (availableHeight <= toolSplit.Panel1MinSize)
			return;

		int toolPanelHeight = Math.Min(
			ThemeMetrics.ToolPanelHeight,
			availableHeight - toolSplit.Panel1MinSize);
		toolSplit.SplitterDistance = availableHeight - Math.Max(toolSplit.Panel2MinSize, toolPanelHeight);
	}
}
