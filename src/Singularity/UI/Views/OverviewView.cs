// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Core.Workloads;
using Singularity.Hardware.Models;
using Singularity.Monitoring.Models;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class OverviewView : Panel
{
	private readonly MetricTile readinessTile = new("Readiness");
	private readonly MetricTile platformTile = new("Platform");
	private readonly MetricTile cpuTile = new("CPU");
	private readonly MetricTile memoryTile = new("Memory");
	private readonly MetricTile gpuTile = new("GPU");
	private readonly MetricTile storageTile = new("Storage");
	private readonly MetricTile qualificationTile = new("Last qualification");
	private readonly FlowLayoutPanel tilesPanel = new();

	private string cpuDetail = "Inventory not loaded";
	private string memoryDetail = "Inventory not loaded";
	private string gpuDetail = "Inventory not loaded";
	private bool inventoryAvailable;

	public OverviewView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Workspace;
		AutoScroll = true;

		Panel header = BuildHeader();

		tilesPanel.Dock = DockStyle.Fill;
		tilesPanel.AutoScroll = true;
		tilesPanel.WrapContents = true;
		tilesPanel.FlowDirection = FlowDirection.LeftToRight;
		tilesPanel.BackColor = Theme.Workspace;
		tilesPanel.Padding = new Padding(ThemeMetrics.SpacingLarge);
		tilesPanel.Controls.AddRange([
			readinessTile,
			platformTile,
			cpuTile,
			memoryTile,
			gpuTile,
			storageTile,
			qualificationTile
		]);

		foreach (Control control in tilesPanel.Controls)
			control.Margin = new Padding(0, 0, ThemeMetrics.Spacing, ThemeMetrics.Spacing);

		Controls.Add(tilesPanel);
		Controls.Add(header);

		UpdateInventory(null);
		UpdateQualification(new WorkloadStatus(), null);
	}

	public event Action? QualificationRequested;

	public void UpdateInventory(PlatformInventoryState? state)
	{
		HardwareInventory? inventory = state?.Current;
		inventoryAvailable = inventory is not null;

		if (inventory is null)
		{
			string detail = state?.Status switch
			{
				PlatformInventoryStatus.Refreshing => "Discovering hardware",
				PlatformInventoryStatus.Failed => state.LastError ?? "Hardware discovery failed",
				_ => "Hardware inventory not loaded"
			};

			platformTile.SetValue("Unavailable", detail, state?.Status == PlatformInventoryStatus.Failed ? Theme.Failure : null);
			cpuDetail = detail;
			memoryDetail = detail;
			gpuDetail = detail;
			cpuTile.SetValue("—", cpuDetail);
			memoryTile.SetValue("—", memoryDetail);
			gpuTile.SetValue("—", gpuDetail);
			storageTile.SetValue("—", detail);
			return;
		}

		string platformDetail = state?.Status == PlatformInventoryStatus.Failed
			? $"Refresh failed • {state.LastError}"
			: $"{inventory.Os.Name} • {inventory.Os.Architecture}";

		platformTile.SetValue(
			inventory.Mainboard.Name,
			platformDetail,
			state?.Status == PlatformInventoryStatus.Failed ? Theme.Failure : null);

		cpuDetail = $"{inventory.Cpu.Name} • {inventory.Cpu.CoreThreadInfo}";
		memoryDetail = $"{inventory.MemoryModules.Count} module{(inventory.MemoryModules.Count == 1 ? "" : "s")}";
		gpuDetail = inventory.Gpus.Count == 0
			? "No supported GPU inventory"
			: string.Join(" • ", inventory.Gpus.Select(gpu => gpu.Name));

		cpuTile.SetValue("Ready", cpuDetail);
		memoryTile.SetValue("Ready", memoryDetail);
		gpuTile.SetValue(inventory.Gpus.Count == 0 ? "Unavailable" : "Ready", gpuDetail);
		storageTile.SetValue(
			$"{inventory.StorageDrives.Count} device{(inventory.StorageDrives.Count == 1 ? "" : "s")}",
			inventory.StorageDrives.Count == 0
				? "No storage devices reported"
				: string.Join(" • ", inventory.StorageDrives.Take(2).Select(storage => storage.Model)));
	}

	public void UpdateTelemetry(SystemSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		string cpuTemperature = snapshot.CpuTemperatureAvailable
			? $"{snapshot.CpuTemperatureCelsius:0} °C"
			: snapshot.CpuTemperatureStatus;
		cpuTile.SetValue($"{snapshot.CpuLoadPercent:0.0} %", $"{cpuDetail} • {cpuTemperature}");

		memoryTile.SetValue(
			$"{snapshot.UsedPhysicalMemoryPercent:0.0} %",
			$"{memoryDetail} • {snapshot.UsedPhysicalMemoryMb:N0} / {snapshot.TotalPhysicalMemoryMb:N0} MB");

		if (snapshot.GpuTelemetryAvailable)
		{
			string power = snapshot.GpuPowerAvailable ? $" • {snapshot.GpuPowerWatts:0} W" : string.Empty;
			gpuTile.SetValue(
				$"{snapshot.GpuLoadPercent:0.0} %",
				$"{gpuDetail} • {snapshot.GpuTemperatureCelsius} °C{power}");
		}
		else
		{
			gpuTile.SetValue(
				inventoryAvailable ? "Unavailable" : "—",
				$"{gpuDetail} • {snapshot.GpuTelemetryStatus}");
		}
	}

	public void UpdateQualification(WorkloadStatus status, QualificationReport? report)
	{
		ArgumentNullException.ThrowIfNull(status);

		(string readiness, Color color, string detail) = status.State switch
		{
			WorkloadState.Starting => ("Starting", Theme.Warning, "Qualification workload is starting"),
			WorkloadState.Running => ("Running", Theme.Success, "Qualification is collecting evidence"),
			WorkloadState.Stopping => ("Stopping", Theme.Warning, "Qualification workload is stopping"),
			WorkloadState.Failed => ("Failed", Theme.Failure, string.IsNullOrWhiteSpace(status.Message) ? "Qualification workload failed" : status.Message),
			_ when !inventoryAvailable => ("Waiting", Theme.Warning, "Platform inventory is required"),
			_ => ("Ready", Theme.Success, "Platform is ready for qualification")
		};

		readinessTile.SetValue(readiness, detail, color);

		if (report is null)
		{
			qualificationTile.SetValue("Not run", "No completed qualification is available");
			return;
		}

		Color resultColor = report.OverallResult switch
		{
			ValidationStatus.Pass => Theme.Success,
			ValidationStatus.Warning => Theme.Warning,
			ValidationStatus.Fail => Theme.Failure,
			_ => Theme.TextMain
		};

		qualificationTile.SetValue(
			report.OverallResult.ToString().ToUpperInvariant(),
			$"{report.Profile.Name} • {report.FinishedAt:g} • {report.Duration.ToString(@"hh\:mm\:ss")}",
			resultColor);
	}

	private Panel BuildHeader()
	{
		Panel header = new()
		{
			Dock = DockStyle.Top,
			Height = 76,
			BackColor = Theme.Workspace,
			Padding = new Padding(ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing, ThemeMetrics.SpacingLarge, ThemeMetrics.Spacing)
		};

		CommandButton qualificationButton = new()
		{
			Dock = DockStyle.Right,
			Width = 176,
			Text = "Open Qualification",
			AccessibleName = "Open Qualification"
		};
		qualificationButton.Click += (_, _) => QualificationRequested?.Invoke();

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 28,
			Text = "Platform overview",
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Workspace
		};

		Label subtitle = new()
		{
			Dock = DockStyle.Fill,
			Text = "Current platform state, live telemetry, and latest qualification evidence.",
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Workspace,
			TextAlign = ContentAlignment.MiddleLeft
		};

		header.Controls.Add(subtitle);
		header.Controls.Add(title);
		header.Controls.Add(qualificationButton);
		return header;
	}
}
