// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;
using Singularity.UI.Navigation;

namespace Singularity.UI.Shell;

public sealed class SidebarHost : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label descriptionLabel = new();
	private readonly FlowLayoutPanel navigationPanel = new();

	public SidebarHost()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Sidebar;
		Padding = new Padding(ThemeMetrics.Spacing, 0, ThemeMetrics.Spacing, ThemeMetrics.Spacing);

		titleLabel.Dock = DockStyle.Top;
		titleLabel.Height = 40;
		titleLabel.Font = ThemeFonts.Header;
		titleLabel.ForeColor = Theme.TextMain;
		titleLabel.BackColor = Theme.Sidebar;
		titleLabel.TextAlign = ContentAlignment.MiddleLeft;

		descriptionLabel.Dock = DockStyle.Top;
		descriptionLabel.Height = 58;
		descriptionLabel.Font = ThemeFonts.Subtitle;
		descriptionLabel.ForeColor = Theme.TextMuted;
		descriptionLabel.BackColor = Theme.Sidebar;
		descriptionLabel.TextAlign = ContentAlignment.TopLeft;

		navigationPanel.Dock = DockStyle.Fill;
		navigationPanel.FlowDirection = FlowDirection.TopDown;
		navigationPanel.WrapContents = false;
		navigationPanel.AutoScroll = true;
		navigationPanel.BackColor = Theme.Sidebar;
		navigationPanel.Padding = new Padding(0, ThemeMetrics.Spacing, 0, 0);

		Controls.Add(navigationPanel);
		Controls.Add(descriptionLabel);
		Controls.Add(titleLabel);
	}

	public void SetContext(
		WorkspaceDefinition workspace,
		NavigationItem? selectedItem,
		Action<NavigationItem> activateItem)
	{
		ArgumentNullException.ThrowIfNull(workspace);
		ArgumentNullException.ThrowIfNull(activateItem);

		titleLabel.Text = workspace.Title.ToUpperInvariant();
		descriptionLabel.Text = workspace.Description;

		while (navigationPanel.Controls.Count > 0)
		{
			Control control = navigationPanel.Controls[0];
			navigationPanel.Controls.RemoveAt(0);
			control.Dispose();
		}

		int width = Math.Max(120, navigationPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
		int tabIndex = 0;

		foreach (NavigationItem item in workspace.SidebarItems)
		{
			CommandButton button = new()
			{
				Text = item.Label,
				AccessibleName = item.Label,
				Width = width,
				Height = ThemeMetrics.ControlHeight,
				Margin = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall),
				TextAlign = ContentAlignment.MiddleLeft,
				TabIndex = tabIndex++,
				BackColor = selectedItem?.Id == item.Id ? Theme.Selected : Theme.Sidebar,
				ForeColor = selectedItem?.Id == item.Id ? Theme.TextMain : Theme.TextMuted
			};

			button.FlatAppearance.BorderSize = selectedItem?.Id == item.Id ? 1 : 0;
			button.FlatAppearance.BorderColor = Theme.Accent;
			button.Click += (_, _) => activateItem(item);
			navigationPanel.Controls.Add(button);
		}
	}
}
