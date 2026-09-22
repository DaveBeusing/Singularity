// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Hardware.Models;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Panels;

namespace Singularity.UI.Views;

public sealed class PlatformView : Panel
{
	private readonly Label stateLabel = new();
	private readonly Label categoryTitleLabel = new();
	private readonly FlowLayoutPanel contentPanel = new();
	private readonly Dictionary<string, DeviceSelectionCard> deviceCards = [];
	private PlatformInventoryState? inventoryState;
	private string category = "system";
	private string? selectedDeviceId;

	public PlatformView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;

		Panel header = BuildHeader();

		contentPanel.Dock = DockStyle.Fill;
		contentPanel.AutoScroll = true;
		contentPanel.WrapContents = false;
		contentPanel.FlowDirection = FlowDirection.TopDown;
		contentPanel.BackColor = Theme.Workspace;
		contentPanel.Padding = new Padding(ThemeMetrics.SpacingLarge);
		contentPanel.Resize += (_, _) => UpdateCardWidths();

		Controls.Add(contentPanel);
		Controls.Add(header);
		Render();
	}

	public CommandButton RefreshButton { get; } = new()
	{
		Text = "Refresh Platform",
		AccessibleName = "Refresh platform inventory",
		Width = 152,
		Dock = DockStyle.Right
	};

	public event Action<PlatformDeviceSelection>? DeviceSelected;

	public void SetCategory(string itemId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

		if (string.Equals(category, itemId, StringComparison.Ordinal))
			return;

		category = itemId;
		selectedDeviceId = null;
		Render();
	}

	public void UpdateInventory(PlatformInventoryState state)
	{
		inventoryState = state ?? throw new ArgumentNullException(nameof(state));
		selectedDeviceId = null;
		UpdateStateLabel();
		Render();
	}

	private Panel BuildHeader()
	{
		Panel header = new()
		{
			Dock = DockStyle.Top,
			Height = 78,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing)
		};

		categoryTitleLabel.Dock = DockStyle.Top;
		categoryTitleLabel.Height = 28;
		categoryTitleLabel.Font = ThemeFonts.Title;
		categoryTitleLabel.ForeColor = Theme.TextMain;
		categoryTitleLabel.BackColor = Theme.Workspace;

		stateLabel.Dock = DockStyle.Fill;
		stateLabel.Font = ThemeFonts.Subtitle;
		stateLabel.ForeColor = Theme.TextMuted;
		stateLabel.BackColor = Theme.Workspace;
		stateLabel.TextAlign = ContentAlignment.MiddleLeft;

		header.Controls.Add(stateLabel);
		header.Controls.Add(categoryTitleLabel);
		header.Controls.Add(RefreshButton);
		return header;
	}

	private void Render()
	{
		ClearContent();
		categoryTitleLabel.Text = CategoryTitle(category);
		UpdateStateLabel();

		HardwareInventory? inventory = inventoryState?.Current;
		if (inventory is null)
		{
			AddEmptyState(inventoryState?.Status switch
			{
				PlatformInventoryStatus.Refreshing => "Discovering platform hardware…",
				PlatformInventoryStatus.Failed => inventoryState.LastError ?? "Platform inventory could not be read.",
				_ => "Platform inventory has not been loaded yet."
			});
			return;
		}

		switch (category)
		{
			case "system":
				AddSystem(inventory);
				break;
			case "cpu":
				AddCard(new CpuInfoPanel(inventory.Cpu, LayoutConstants.HardwareCardWidth, LayoutConstants.LargeCardHeight));
				break;
			case "memory":
				AddMemory(inventory.MemoryModules);
				break;
			case "gpu":
				AddGpus(inventory.Gpus);
				break;
			case "storage":
				AddStorage(inventory.StorageDrives);
				break;
			default:
				AddEmptyState("This platform category is unavailable.");
				break;
		}

		UpdateCardWidths();
	}

	private void AddSystem(HardwareInventory inventory)
	{
		AddCard(new OsInfoPanel(inventory.Os, LayoutConstants.HardwareCardWidth, LayoutConstants.CardHeight));
		AddCard(new MainboardInfoPanel(inventory.Mainboard, LayoutConstants.HardwareCardWidth, LayoutConstants.CardHeight));
	}

	private void AddMemory(IReadOnlyList<MemoryInventory> modules)
	{
		if (modules.Count == 0)
		{
			AddEmptyState("No memory modules were reported by platform inventory.");
			return;
		}

		for (int index = 0; index < modules.Count; index++)
		{
			MemoryInventory memory = modules[index];
			AddSelectableCard(
				new MemoryInfoPanel(memory, LayoutConstants.HardwareCardWidth, LayoutConstants.LargeCardHeight),
				PlatformSelectionMapper.FromMemory(memory, index));
		}
	}

	private void AddGpus(IReadOnlyList<GpuInventory> gpus)
	{
		if (gpus.Count == 0)
		{
			AddEmptyState("No supported GPU inventory is available.");
			return;
		}

		for (int index = 0; index < gpus.Count; index++)
		{
			GpuInventory gpu = gpus[index];
			AddSelectableCard(
				new GpuInfoPanel(gpu, LayoutConstants.HardwareCardWidth, LayoutConstants.LargeCardHeight),
				PlatformSelectionMapper.FromGpu(gpu, index));
		}
	}

	private void AddStorage(IReadOnlyList<StorageInventory> drives)
	{
		if (drives.Count == 0)
		{
			AddEmptyState("No storage devices were reported by platform inventory.");
			return;
		}

		for (int index = 0; index < drives.Count; index++)
		{
			StorageInventory drive = drives[index];
			AddSelectableCard(
				new StorageInfoPanel(drive, LayoutConstants.HardwareCardWidth, LayoutConstants.LargeCardHeight),
				PlatformSelectionMapper.FromStorage(drive, index));
		}
	}

	private void AddCard(Control card)
	{
		card.Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing);
		contentPanel.Controls.Add(card);
	}

	private void AddSelectableCard(Control content, PlatformDeviceSelection selection)
	{
		DeviceSelectionCard card = new(content)
		{
			Margin = new Padding(0, 0, 0, ThemeMetrics.Spacing),
			AccessibleName = $"{selection.Kind}: {selection.DisplayName}"
		};

		card.Selected = string.Equals(selectedDeviceId, selection.Id, StringComparison.Ordinal);
		card.SelectionRequested += () => SelectDevice(card, selection);
		deviceCards[selection.Id] = card;
		contentPanel.Controls.Add(card);
	}

	private void SelectDevice(DeviceSelectionCard card, PlatformDeviceSelection selection)
	{
		selectedDeviceId = selection.Id;

		foreach (DeviceSelectionCard candidate in deviceCards.Values)
			candidate.Selected = ReferenceEquals(candidate, card);

		DeviceSelected?.Invoke(selection);
	}

	private void AddEmptyState(string text)
	{
		Label empty = new()
		{
			Width = LayoutConstants.HardwareCardWidth,
			Height = 90,
			Text = text,
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleCenter,
			Margin = Padding.Empty,
			Padding = new Padding(ThemeMetrics.Spacing)
		};
		contentPanel.Controls.Add(empty);
	}

	private void UpdateStateLabel()
	{
		if (inventoryState is null)
		{
			stateLabel.Text = "Platform inventory is waiting to start.";
			stateLabel.ForeColor = Theme.TextMuted;
			return;
		}

		stateLabel.Text = inventoryState.Status switch
		{
			PlatformInventoryStatus.Refreshing when inventoryState.Current is not null =>
				"Refreshing platform inventory; the last valid inventory remains visible.",
			PlatformInventoryStatus.Refreshing => "Discovering platform hardware…",
			PlatformInventoryStatus.Available when inventoryState.LastUpdatedAtUtc is { } updated =>
				$"Inventory available • refreshed {updated.ToLocalTime():g}",
			PlatformInventoryStatus.Failed when inventoryState.Current is not null =>
				$"Refresh failed; showing last valid inventory • {inventoryState.LastError}",
			PlatformInventoryStatus.Failed => inventoryState.LastError ?? "Platform inventory is unavailable.",
			_ => "Platform inventory has not been loaded."
		};

		stateLabel.ForeColor = inventoryState.Status == PlatformInventoryStatus.Failed
			? Theme.Failure
			: Theme.TextMuted;
	}

	private void ClearContent()
	{
		deviceCards.Clear();

		while (contentPanel.Controls.Count > 0)
		{
			Control control = contentPanel.Controls[0];
			contentPanel.Controls.RemoveAt(0);
			control.Dispose();
		}
	}

	private void UpdateCardWidths()
	{
		int availableWidth = Math.Max(
			LayoutConstants.HardwareCardWidth,
			contentPanel.ClientSize.Width - contentPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);

		foreach (Control control in contentPanel.Controls)
		{
			control.Width = availableWidth;
		}
	}

	private static string CategoryTitle(string categoryId)
	{
		return categoryId switch
		{
			"system" => "System",
			"cpu" => "CPU",
			"memory" => "Memory",
			"gpu" => "GPU",
			"storage" => "Storage",
			_ => "Platform"
		};
	}
}
