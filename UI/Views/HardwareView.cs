// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Providers;
using Singularity.Hardware.Models;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class HardwareView : Panel
{
	private readonly HardwareProvider hardwareProvider = new();

	public HardwareView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		BuildUi();
	}

	private void BuildUi()
	{
		Controls.Clear();

		HardwareInventory inventory = hardwareProvider.Read();

		OsSection osSection = new(inventory.Os)
		{
			Left = 0,
			Top = 0
		};

		HardwareSection hardwareSection = new(inventory)
		{
			Left = 0,
			Top = osSection.Bottom + LayoutConstants.SectionGap
		};

		Controls.AddRange([
			osSection,
			hardwareSection
		]);

		Height = hardwareSection.Bottom;
	}
}