// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public sealed class ToolButton : CommandButton
{
	public ToolButton()
	{
		Width = ThemeMetrics.ActivityButtonSize;
		Height = ThemeMetrics.ActivityButtonSize;
		BackColor = Theme.ActivityBar;
		ForeColor = Theme.TextMuted;
		Font = ThemeFonts.SectionHeader;
		Margin = Padding.Empty;
		Padding = Padding.Empty;
	}
}
