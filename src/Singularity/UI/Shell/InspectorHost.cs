// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Shell;

public sealed class InspectorHost : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label placeholderLabel = new();

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

		Controls.Add(placeholderLabel);
		Controls.Add(titleLabel);
	}

	public void SetContent(Control? content)
	{
		if (Controls.Count > 1 && Controls[0] != placeholderLabel)
			Controls.RemoveAt(0);

		placeholderLabel.Visible = content is null;

		if (content is null)
			return;

		content.Dock = DockStyle.Fill;
		Controls.Add(content);
		content.BringToFront();
		titleLabel.BringToFront();
	}
}
