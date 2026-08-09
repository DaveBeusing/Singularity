// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class OsSection : Panel
{
	public OsSection(OsInventory osInventory)
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		Height = 150;
		BackColor = Theme.Panel;

		BuildUi(osInventory);
	}

	private void BuildUi(OsInventory osInventory)
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"OS");

		OsInfoPanel osInfoPanel = new(
			osInventory,
			LayoutConstants.HardwareCardWidth,
			LayoutConstants.CardHeight)
		{
			Left = LayoutConstants.HardwareCardLeft,
			Top = LayoutConstants.SectionHeaderHeight
		};

		Controls.Add(osInfoPanel);
	}
}