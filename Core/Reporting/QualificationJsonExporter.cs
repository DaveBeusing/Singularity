// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Singularity.Hardware.Models;

namespace Singularity.Core.Reporting;

public sealed class QualificationJsonExporter
{
	public const string SchemaVersion = "1.0";

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() }
	};

	public string Serialize(
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion)
	{
		QualificationJsonDocument document = CreateDocument(report, hardware, singularityVersion);

		return JsonSerializer.Serialize(document, SerializerOptions);
	}

	internal static QualificationJsonDocument CreateDocument(
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion)
	{
		ArgumentNullException.ThrowIfNull(report);
		ArgumentNullException.ThrowIfNull(hardware);

		return new QualificationJsonDocument(
			SchemaVersion,
			singularityVersion,
			report.FinishedAt,
			report.Duration,
			report.Profile,
			new QualificationValidationJson(
				report.CpuResult,
				report.MemoryResult,
				report.GpuResult,
				report.OverallResult),
			report.TelemetryStatistics,
			CreateHardwareSummary(hardware));
	}

	public void Export(
		string path,
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		File.WriteAllText(path, Serialize(report, hardware, singularityVersion));
	}

	private static HardwareSummaryJson CreateHardwareSummary(HardwareInventory hardware)
	{
		GpuSummaryJson[] gpus = new GpuSummaryJson[hardware.Gpus.Count];
		for (int index = 0; index < hardware.Gpus.Count; index++)
		{
			GpuInventory gpu = hardware.Gpus[index];
			gpus[index] = new GpuSummaryJson(
				gpu.Identifier,
				gpu.Name,
				gpu.Vram,
				$"Gen{gpu.PcieGenerationCurrent} x{gpu.PcieWidthCurrent}");
		}

		string[] memory = new string[hardware.MemoryModules.Count];
		for (int index = 0; index < hardware.MemoryModules.Count; index++)
		{
			MemoryInventory module = hardware.MemoryModules[index];
			memory[index] = $"{module.Slot}: {module.Capacity} {module.MemoryType} @ {module.Speed}";
		}

		string[] storage = new string[hardware.StorageDrives.Count];
		for (int index = 0; index < hardware.StorageDrives.Count; index++)
			storage[index] = hardware.StorageDrives[index].Details;

		return new HardwareSummaryJson(
			hardware.Os.DisplayVersion,
			hardware.Os.ComputerName,
			hardware.Mainboard.Name,
			hardware.Cpu.Name,
			hardware.Cpu.CoreCount,
			hardware.Cpu.ThreadCount,
			Array.AsReadOnly(gpus),
			Array.AsReadOnly(memory),
			Array.AsReadOnly(storage));
	}
}
