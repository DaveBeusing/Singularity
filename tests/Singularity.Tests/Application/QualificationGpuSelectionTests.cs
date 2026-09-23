// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Hardware.Models;

namespace Singularity.Tests.Application;

public sealed class QualificationGpuSelectionTests
{
	[Fact]
	public void CreateOptions_FiltersTransientFallbackIdentity()
	{
		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(
			[
				new GpuInventory { Identifier = "nvml:0", Name = "Transient" },
				new GpuInventory { Identifier = "GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", Name = "Stable" }
			]);

		QualificationGpuOption option = Assert.Single(options);
		Assert.Equal("GPU-AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", option.Identifier);
	}

	[Fact]
	public void CreateOptions_AcceptsVendorNeutralLuidIdentity()
	{
		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(
			[
				new GpuInventory
				{
					Identifier = "dxgi:luid:0000000000000042",
					Name = "AMD Radeon",
					IsNvidia = false,
					IsDirect3D12Capable = true
				}
			]);

		QualificationGpuOption option = Assert.Single(options);
		Assert.Equal("dxgi:luid:0000000000000042", option.Identifier);
	}

	[Fact]
	public void CreateOptions_FiltersAdapterWithoutDirect3D12Support()
	{
		IReadOnlyList<QualificationGpuOption> options =
			QualificationGpuSelection.CreateOptions(
			[
				new GpuInventory
				{
					Identifier = "dxgi:luid:0000000000000042",
					Name = "Legacy Adapter",
					IsDirect3D12Capable = false
				}
			]);

		Assert.Empty(options);
	}

	[Fact]
	public void ResolveSelection_DefaultsDeterministicallyAndUsesIdentifierAfterReorder()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");
		QualificationGpuOption gpuB = new("GPU-B", "GPU B");

		Assert.Equal(gpuA, QualificationGpuSelection.ResolveSelection([gpuA, gpuB], null));
		Assert.Equal(gpuB, QualificationGpuSelection.ResolveSelection([gpuB, gpuA], "GPU-B"));
	}

	[Fact]
	public void ResolveSelection_ReturnsNullForStaleIdentifier()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");

		Assert.Null(QualificationGpuSelection.ResolveSelection([gpuA], "GPU-REMOVED"));
	}

	[Fact]
	public void ResolveSelections_PreservesRequestedDeviceIdentities()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");
		QualificationGpuOption gpuB = new("GPU-B", "GPU B");

		IReadOnlyList<QualificationGpuOption> selected =
			QualificationGpuSelection.ResolveSelections(
				[gpuB, gpuA],
				["GPU-A", "GPU-B"]);

		Assert.Equal(["GPU-A", "GPU-B"], selected.Select(item => item.Identifier));
	}

	[Fact]
	public void ResolveSelections_DropsOnlyStaleDevices()
	{
		QualificationGpuOption gpuA = new("GPU-A", "GPU A");

		IReadOnlyList<QualificationGpuOption> selected =
			QualificationGpuSelection.ResolveSelections(
				[gpuA],
				["GPU-A", "GPU-REMOVED"]);

		Assert.Equal(["GPU-A"], selected.Select(item => item.Identifier));
	}
}
