// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;

namespace Singularity.UI.Shell;

public sealed class ApplicationStatusBar : Panel
{
	private readonly Label contextLabel = new();
	private readonly StatusIndicator statusIndicator = new();

	public ApplicationStatusBar()
	{
		Dock = DockStyle.Bottom;
		Height = ThemeMetrics.StatusBarHeight;
		BackColor = Theme.StatusBar;

		contextLabel.Dock = DockStyle.Fill;
		contextLabel.Text = "Platform qualification";
		contextLabel.Padding = new Padding(ThemeMetrics.Spacing, 0, 0, 0);
		contextLabel.Font = ThemeFonts.SectionHeader;
		contextLabel.ForeColor = Theme.TextMuted;
		contextLabel.BackColor = Theme.StatusBar;
		contextLabel.TextAlign = ContentAlignment.MiddleLeft;

		statusIndicator.Dock = DockStyle.Right;

		Controls.Add(contextLabel);
		Controls.Add(statusIndicator);
	}

	public void SetContext(string context)
	{
		ControlUpdate.SetText(contextLabel, context);
	}

	public void SetStatus(string text, StatusVisualState state)
	{
		statusIndicator.SetState(text, state);
	}
}
