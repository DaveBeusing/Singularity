// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;

namespace Singularity.UI.Shell;

public sealed class ApplicationShell : UserControl
{
	private readonly ActivityBar activityBar = new();
	private readonly SidebarHost sidebarHost = new();
	private readonly WorkspaceHost workspaceHost = new();
	private readonly InspectorHost inspectorHost = new();
	private readonly ToolPanelHost toolPanelHost = new();
	private readonly ApplicationStatusBar statusBar = new();

	private readonly SplitContainer bodySplit = new();
	private readonly SplitContainer inspectorSplit = new();
	private readonly SplitContainer toolSplit = new();

	private int savedSidebarWidth = ThemeMetrics.DefaultSidebarWidth;
	private bool applyingLayoutState;

	public ApplicationShell(string version)
	{
		AutoScaleMode = AutoScaleMode.Dpi;
		BackColor = Theme.ApplicationBackground;
		Size = new Size(ThemeMetrics.DefaultWindowWidth, ThemeMetrics.DefaultWindowHeight);

		BuildLayout(version);
		WireInteractions();

		LayoutState = ShellLayoutState.Default;
		PerformLayout();
		ApplyLayoutState();
	}

	public ShellLayoutState LayoutState { get; private set; }
	public WorkspaceHost Workspace => workspaceHost;
	public SidebarHost Sidebar => sidebarHost;
	public InspectorHost Inspector => inspectorHost;
	public ToolPanelHost ToolPanel => toolPanelHost;

	public event Action<ShellSection>? ActiveSectionChanged;

	public void RegisterWorkspace(ShellSection section, Control content)
	{
		workspaceHost.Register(section, content);
	}

	public void ActivateSection(ShellSection section)
	{
		workspaceHost.Activate(section);
		activityBar.SetActive(section);

		switch (section)
		{
			case ShellSection.Platform:
				sidebarHost.SetContext("PLATFORM", "Hardware inventory and platform details.");
				statusBar.SetContext("Platform");
				break;

			case ShellSection.Workloads:
				sidebarHost.SetContext("WORKLOADS", "Qualification controls, telemetry, results, and history.");
				statusBar.SetContext("Workloads");
				break;
		}

		ActiveSectionChanged?.Invoke(section);
	}

	public void SetGlobalStatus(string text, StatusVisualState state)
	{
		statusBar.SetStatus(text, state);
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
	}

	public void SetInspectorVisible(bool visible)
	{
		if (LayoutState.InspectorVisible == visible)
			return;

		LayoutState = LayoutState.WithInspector(visible);
		ApplyLayoutState();
	}

	public void SetToolPanelVisible(bool visible)
	{
		if (LayoutState.ToolPanelVisible == visible)
			return;

		LayoutState = LayoutState.WithToolPanel(visible);
		ApplyLayoutState();
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

	private static Panel BuildHeader(string version)
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

		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Platform Qualification Suite",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.ApplicationBackground,
			TextAlign = ContentAlignment.MiddleLeft
		};

		SectionSeparator separator = new()
		{
			Dock = DockStyle.Bottom
		};

		header.Controls.Add(subtitle);
		header.Controls.Add(versionLabel);
		header.Controls.Add(title);
		header.Controls.Add(separator);
		return header;
	}

	private void WireInteractions()
	{
		activityBar.NavigationRequested += ActivateSection;
		activityBar.ToggleSidebarRequested += () => SetSidebarVisible(!LayoutState.SidebarVisible);
		activityBar.ToggleInspectorRequested += () => SetInspectorVisible(!LayoutState.InspectorVisible);
		activityBar.ToggleToolPanelRequested += () => SetToolPanelVisible(!LayoutState.ToolPanelVisible);
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

	private void ApplyLayoutState()
	{
		if (bodySplit.ClientSize.Width <= 0 || bodySplit.ClientSize.Height <= 0)
			return;

		applyingLayoutState = true;
		try
		{
			sidebarHost.Visible = LayoutState.SidebarVisible;

			int desiredNavigationWidth = ThemeMetrics.ActivityBarWidth;
			if (LayoutState.SidebarVisible)
				desiredNavigationWidth += savedSidebarWidth;

			int requiredContentWidth = ThemeMetrics.MinimumWorkspaceWidth;
			if (LayoutState.InspectorVisible)
			{
				requiredContentWidth += inspectorSplit.SplitterWidth + inspectorSplit.Panel2MinSize;
			}

			int maximumNavigationWidth = Math.Max(
				ThemeMetrics.ActivityBarWidth,
				bodySplit.ClientSize.Width - bodySplit.SplitterWidth - requiredContentWidth);
			bodySplit.SplitterDistance = Math.Min(desiredNavigationWidth, maximumNavigationWidth);
			bodySplit.PerformLayout();

			inspectorSplit.Panel2Collapsed = !LayoutState.InspectorVisible;
			if (LayoutState.InspectorVisible)
				ApplyInspectorWidth();

			toolSplit.Panel2Collapsed = !LayoutState.ToolPanelVisible;
			if (LayoutState.ToolPanelVisible)
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
