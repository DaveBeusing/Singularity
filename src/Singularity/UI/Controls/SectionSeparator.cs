// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public sealed class SectionSeparator : Panel
{
	public SectionSeparator()
	{
		Height = 1;
		BackColor = Theme.Separator;
		Margin = Padding.Empty;
	}
}
