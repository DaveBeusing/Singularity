// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;

namespace Singularity.Application;

public enum PlatformInventoryStatus
{
	NotLoaded,
	Refreshing,
	Available,
	Failed
}

public sealed class PlatformInventoryState
{
	private readonly Func<HardwareInventory> readInventory;
	private readonly SemaphoreSlim refreshGate = new(1, 1);

	public PlatformInventoryState(Func<HardwareInventory> readInventory)
	{
		this.readInventory = readInventory ?? throw new ArgumentNullException(nameof(readInventory));
	}

	public HardwareInventory? Current { get; private set; }
	public PlatformInventoryStatus Status { get; private set; } = PlatformInventoryStatus.NotLoaded;
	public DateTime? LastUpdatedAtUtc { get; private set; }
	public string? LastError { get; private set; }

	public bool HasInventory => Current is not null;
	public bool IsRefreshing => Status == PlatformInventoryStatus.Refreshing;

	public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
	{
		if (!await refreshGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
			return false;

		try
		{
			Status = PlatformInventoryStatus.Refreshing;
			LastError = null;
			HardwareInventory inventory = await Task.Run(readInventory, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			Current = inventory ?? throw new InvalidOperationException("Hardware inventory provider returned no inventory.");
			LastUpdatedAtUtc = DateTime.UtcNow;
			Status = PlatformInventoryStatus.Available;
			return true;
		}
		catch (OperationCanceledException)
		{
			Status = Current is null
				? PlatformInventoryStatus.NotLoaded
				: PlatformInventoryStatus.Available;
			throw;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			Status = PlatformInventoryStatus.Failed;
			return false;
		}
		finally
		{
			refreshGate.Release();		}
	}
}
