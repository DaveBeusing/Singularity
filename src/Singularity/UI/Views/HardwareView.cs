// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Providers;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class HardwareView : Panel
{
	private readonly HardwareProvider hardwareProvider = new();

	public HardwareInventory Inventory { get; private set; } = new();

	public HardwareView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		ApplyInventory(hardwareProvider.Read());
	}

	public async Task RefreshInventoryAsync(CancellationToken cancellationToken = default)
	{
		HardwareInventory inventory = await Task.Run(
			hardwareProvider.Read,
			cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();
		ApplyInventory(inventory);
	}

	private void ApplyInventory(HardwareInventory inventory)
	{
		SuspendLayout();
		try
		{
			Controls.Clear();
			Inventory = inventory;

			OsSection osSection = new(Inventory.Os)
			{
				Left = 0,
				Top = 0
			};

			HardwareSection hardwareSection = new(Inventory)
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
		finally
		{
			ResumeLayout(true);
		}
	}
}
