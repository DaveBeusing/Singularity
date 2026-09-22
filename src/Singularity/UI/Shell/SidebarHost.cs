// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Shell;

public sealed class SidebarHost : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label descriptionLabel = new();
	private readonly Panel contentHost = new();

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

		contentHost.Dock = DockStyle.Fill;
		contentHost.BackColor = Theme.Sidebar;
		contentHost.Padding = new Padding(0, ThemeMetrics.Spacing, 0, 0);

		Controls.Add(contentHost);
		Controls.Add(descriptionLabel);
		Controls.Add(titleLabel);
	}

	public void SetContext(string title, string description)
	{
		titleLabel.Text = title;
		descriptionLabel.Text = description;
	}

	public void SetContent(Control? content)
	{
		contentHost.Controls.Clear();

		if (content is null)
			return;

		content.Dock = DockStyle.Fill;
		contentHost.Controls.Add(content);
	}
}
