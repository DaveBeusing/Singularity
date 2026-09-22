// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Workloads;
using Singularity.Hardware.Providers;
using Singularity.Monitoring.Runtime;
using Singularity.UI;

namespace Singularity;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();
		using WorkloadManager workloadManager = new();
		using SystemMonitor systemMonitor = new();
		QualificationCoordinator coordinator = new(workloadManager);
		ReportExportService reportExportService = new();
		HardwareProvider hardwareProvider = new();
		PlatformInventoryState platformInventoryState = new(hardwareProvider.Read);
		QualificationWorkspaceState qualificationWorkspaceState = new();
		System.Windows.Forms.Application.Run(
			new MainForm(
				coordinator,
				reportExportService,
				systemMonitor,
				platformInventoryState,
				qualificationWorkspaceState));
	}
}
