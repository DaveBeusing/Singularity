// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using Singularity.Monitoring.Models;

namespace Singularity.Monitoring.Providers;

public sealed class LibreHardwareCpuTelemetryProvider : IDisposable
{
	private readonly Computer? computer;
	private readonly string initializationStatus;
	private bool disposed;
	private bool sensorDumpWritten;

	public LibreHardwareCpuTelemetryProvider()
	{
		Computer? candidate = null;
		try
		{
			candidate = new Computer
			{
				IsCpuEnabled = true,
				IsMotherboardEnabled = true,
				IsControllerEnabled = true
			};

			candidate.Open();
			computer = candidate;
			initializationStatus = "OK";
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"CPU telemetry initialization error: {ex}");
			try
			{
				candidate?.Close();
			}
			catch
			{
				// Best effort cleanup after failed hardware initialization.
			}

			computer = null;
			initializationStatus = "CPU temp initialization failed";
		}
	}

	public CpuTelemetrySnapshot Read()
	{
		Computer? activeComputer = computer;
		if (activeComputer is null)
		{
			return new CpuTelemetrySnapshot
			{
				IsAvailable = false,
				Status = initializationStatus
			};
		}

		try
		{
			UpdateAllHardware(activeComputer);

			if (!sensorDumpWritten)
			{
				WriteSensorDump(activeComputer);
				sensorDumpWritten = true;
			}

			ISensor? sensor =
				FindPreferredCpuTemperatureSensor(activeComputer)
				?? FindAnyCpuTemperatureSensor(activeComputer);

			if (sensor?.Value is float temperature)
			{
				return new CpuTelemetrySnapshot
				{
					IsAvailable = true,
					TemperatureCelsius = temperature,
					Status = sensor.Name
				};
			}

			return new CpuTelemetrySnapshot
			{
				IsAvailable = false,
				Status = "N/A °C"
			};
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"CPU telemetry error: {ex}");

			return new CpuTelemetrySnapshot
			{
				IsAvailable = false,
				Status = "CPU temp read failed"
			};
		}
	}

	private static void UpdateAllHardware(Computer computer)
	{
		foreach (IHardware hardware in computer.Hardware)
		{
			UpdateHardwareRecursive(hardware);
		}
	}

	private static void UpdateHardwareRecursive(IHardware hardware)
	{
		hardware.Update();

		foreach (IHardware subHardware in hardware.SubHardware)
		{
			UpdateHardwareRecursive(subHardware);
		}
	}

	private static ISensor? FindPreferredCpuTemperatureSensor(Computer computer)
	{
		string[] preferredNames =
		[
			"CPU Package",
			"Package",
			"Core Max",
			"Core Average",
			"Tctl",
			"Tdie",
			"CCD",
			"Core #1",
			"Core 1"
		];

		foreach (string preferredName in preferredNames)
		{
			foreach (ISensor sensor in EnumerateSensors(computer))
			{
				if (sensor.SensorType != SensorType.Temperature)
					continue;

				if (!sensor.Value.HasValue)
					continue;

				if (!IsCpuRelated(sensor))
					continue;

				if (sensor.Name.Contains(
					preferredName,
					StringComparison.OrdinalIgnoreCase))
				{
					return sensor;
				}
			}
		}

		return null;
	}

	private static ISensor? FindAnyCpuTemperatureSensor(Computer computer)
	{
		foreach (ISensor sensor in EnumerateSensors(computer))
		{
			if (sensor.SensorType != SensorType.Temperature)
				continue;

			if (!sensor.Value.HasValue)
				continue;

			if (IsCpuRelated(sensor))
				return sensor;
		}

		return null;
	}

	private static bool IsCpuRelated(ISensor sensor)
	{
		string hardwareName = sensor.Hardware.Name;
		string sensorName = sensor.Name;

		return sensor.Hardware.HardwareType == HardwareType.Cpu
			|| hardwareName.Contains("CPU", StringComparison.OrdinalIgnoreCase)
			|| hardwareName.Contains("Intel", StringComparison.OrdinalIgnoreCase)
			|| hardwareName.Contains("AMD", StringComparison.OrdinalIgnoreCase)
			|| sensorName.Contains("CPU", StringComparison.OrdinalIgnoreCase)
			|| sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase)
			|| sensorName.Contains("Package", StringComparison.OrdinalIgnoreCase)
			|| sensorName.Contains("Tctl", StringComparison.OrdinalIgnoreCase)
			|| sensorName.Contains("Tdie", StringComparison.OrdinalIgnoreCase);
	}

	private static IEnumerable<ISensor> EnumerateSensors(Computer computer)
	{
		foreach (IHardware hardware in computer.Hardware)
		{
			foreach (ISensor sensor in EnumerateSensorsRecursive(hardware))
			{
				yield return sensor;
			}
		}
	}

	private static IEnumerable<ISensor> EnumerateSensorsRecursive(IHardware hardware)
	{
		foreach (ISensor sensor in hardware.Sensors)
		{
			yield return sensor;
		}

		foreach (IHardware subHardware in hardware.SubHardware)
		{
			foreach (ISensor sensor in EnumerateSensorsRecursive(subHardware))
			{
				yield return sensor;
			}
		}
	}

	private static void WriteSensorDump(Computer computer)
	{
		Debug.WriteLine("===== LibreHardwareMonitor Sensor Dump =====");

		foreach (IHardware hardware in computer.Hardware)
		{
			WriteHardwareDump(hardware, 0);
		}

		Debug.WriteLine("===== End Sensor Dump =====");
	}

	private static void WriteHardwareDump(IHardware hardware, int depth)
	{
		string indent = new(' ', depth * 2);

		Debug.WriteLine(
			$"{indent}Hardware: {hardware.HardwareType} | {hardware.Name}");

		foreach (ISensor sensor in hardware.Sensors)
		{
			Debug.WriteLine(
				$"{indent}  Sensor: {sensor.SensorType} | {sensor.Name} | {sensor.Value}");
		}

		foreach (IHardware subHardware in hardware.SubHardware)
		{
			WriteHardwareDump(subHardware, depth + 1);
		}
	}

	public void Dispose()
	{
		if (disposed)
			return;

		if (computer is not null)
		{
			try
			{
				computer.Close();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"CPU telemetry shutdown error: {ex}");
			}
		}
		disposed = true;
	}
}
