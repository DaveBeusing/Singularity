// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Layout;

namespace Singularity.UI.Views;

public sealed class WorkspacePlaceholderView : Panel
{
	public WorkspacePlaceholderView(string title, string description)
	{
		Width = LayoutConstants.MainWidth;
		Height = 240;
		BackColor = Theme.Background;

		Label titleLabel = new()
		{
			Left = 0,
			Top = 8,
			Width = LayoutConstants.MainWidth,
			Height = 36,
			Text = title,
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label descriptionLabel = new()
		{
			Left = 0,
			Top = 54,
			Width = Math.Min(620, LayoutConstants.MainWidth),
			Height = 72,
			Text = description,
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.TopLeft
		};

		Controls.AddRange([titleLabel, descriptionLabel]);
	}
}
