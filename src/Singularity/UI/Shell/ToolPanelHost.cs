// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Shell;

public sealed class ToolPanelHost : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label placeholderLabel = new();
	private readonly Panel contentHost = new();

	public ToolPanelHost()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.ToolPanel;
		Padding = new Padding(ThemeMetrics.Spacing);

		titleLabel.Dock = DockStyle.Top;
		titleLabel.Height = 32;
		titleLabel.Text = "TOOLS";
		titleLabel.Font = ThemeFonts.Header;
		titleLabel.ForeColor = Theme.TextMain;
		titleLabel.BackColor = Theme.ToolPanel;
		titleLabel.TextAlign = ContentAlignment.MiddleLeft;

		placeholderLabel.Dock = DockStyle.Fill;
		placeholderLabel.Text = "Tool and telemetry output can be hosted here.";
		placeholderLabel.Font = ThemeFonts.Subtitle;
		placeholderLabel.ForeColor = Theme.TextMuted;
		placeholderLabel.BackColor = Theme.ToolPanel;
		placeholderLabel.TextAlign = ContentAlignment.TopLeft;

		contentHost.Dock = DockStyle.Fill;
		contentHost.BackColor = Theme.ToolPanel;
		contentHost.Controls.Add(placeholderLabel);

		Controls.Add(contentHost);
		Controls.Add(titleLabel);
	}

	public void SetContent(Control? content)
	{
		contentHost.Controls.Clear();

		if (content is null)
		{
			contentHost.Controls.Add(placeholderLabel);
			return;
		}

		content.Dock = DockStyle.Fill;
		contentHost.Controls.Add(content);
	}
}
