// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Layout;

namespace Singularity.UI.Controls;

public sealed class SectionHeader : Panel
{
	public SectionHeader(SingularityIconType iconType, string title)
	{
		Left = 0;
		Top = 0;
		Height = LayoutConstants.SectionHeaderHeight;
		BackColor = Theme.Panel;

		SingularityIcon icon = new()
		{
			IconType = iconType,
			IconColor = Theme.Accent,
			Left = LayoutConstants.SectionPadding,
			Top = 14,
			Width = 32,
			Height = 32,
			BackColor = Theme.Panel
		};

		Label titleLabel = new()
		{
			Text = title,
			Left = 60,
			Top = 14,
			Width = 250,
			Height = 25,
			Font = ThemeFonts.SectionHeader,
			ForeColor = Theme.Accent,
			BackColor = Theme.Panel
		};

		Controls.AddRange([icon, titleLabel]);
	}
}
