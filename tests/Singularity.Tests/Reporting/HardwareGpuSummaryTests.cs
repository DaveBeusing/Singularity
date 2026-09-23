// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Hardware.Models;

namespace Singularity.Tests.Reporting;

public sealed class HardwareGpuSummaryTests
{
	[Fact]
	public void JsonHardwareSummaryRepresentsVendorNeutralGpuWithUnavailablePcie()
	{
		QualificationReport report = new()
		{
			StartedAt = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc),
			FinishedAt = new DateTime(2026, 9, 23, 12, 1, 0, DateTimeKind.Utc),
			Duration = TimeSpan.FromMinutes(1),
			OverallResult = ValidationStatus.Pass
		};
		HardwareInventory hardware = new()
		{
			Gpus =
			[
				new GpuInventory
				{
					Identifier = "dxgi:luid:0000000000000042",
					Name = "AMD Radeon Example",
					Vendor = "AMD",
					IsNvidia = false,
					Vram = "VRAM 16GB",
					PcieGenerationCurrent = "Unavailable",
					PcieWidthCurrent = "Unavailable"
				}
			]
		};

		string json = new QualificationJsonExporter().Serialize(
			report,
			hardware,
			"1.2.3");

		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement gpu = document.RootElement
			.GetProperty("hardware")
			.GetProperty("gpus")[0];

		Assert.Equal("AMD Radeon Example", gpu.GetProperty("name").GetString());
		Assert.Equal("dxgi:luid:0000000000000042", gpu.GetProperty("identifier").GetString());
		Assert.Equal("VRAM 16GB", gpu.GetProperty("vram").GetString());
		Assert.Equal("Unavailable", gpu.GetProperty("pcieLink").GetString());
	}
}
