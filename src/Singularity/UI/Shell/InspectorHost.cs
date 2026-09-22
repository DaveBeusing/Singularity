// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Navigation;

namespace Singularity.UI.Shell;

public sealed class InspectorHost : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label placeholderLabel = new();
	private readonly Panel contentHost = new();

	public InspectorHost()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Inspector;
		Padding = new Padding(ThemeMetrics.Spacing);

		titleLabel.Dock = DockStyle.Top;
		titleLabel.Height = 36;
		titleLabel.Text = "INSPECTOR";
		titleLabel.Font = ThemeFonts.Header;
		titleLabel.ForeColor = Theme.TextMain;
		titleLabel.BackColor = Theme.Inspector;
		titleLabel.TextAlign = ContentAlignment.MiddleLeft;

		placeholderLabel.Dock = DockStyle.Fill;
		placeholderLabel.Text = "No contextual details available.";
		placeholderLabel.Font = ThemeFonts.Subtitle;
		placeholderLabel.ForeColor = Theme.TextMuted;
		placeholderLabel.BackColor = Theme.Inspector;
		placeholderLabel.TextAlign = ContentAlignment.TopLeft;

		contentHost.Dock = DockStyle.Fill;
		contentHost.BackColor = Theme.Inspector;
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

	public void SetSelection(WorkspaceSelection? selection)
	{
		if (contentHost.Controls.Count != 1 || contentHost.Controls[0] != placeholderLabel)
			return;

		placeholderLabel.Text = selection is null
			? "No contextual details available."
			: $"{selection.Kind}: {selection.DisplayName}";
	}
}
