// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Application.Persistence;
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
		try
		{
			ApplicationConfiguration.Initialize();
			StartupFailureReporter.Install();
			RunApplication();
		}
		catch (Exception ex)
		{
			StartupFailureReporter.ReportStartupFailure(ex);
		}
	}

	private static void RunApplication()
	{
		using WorkloadManager workloadManager = new();
		using SystemMonitor systemMonitor = new();
		using QualificationArchiveService qualificationArchive = new();
		using QualificationProfileStore profileStore = new();
		QualificationProfileCatalog profileCatalog = new(profileStore);
		QualificationCoordinator coordinator = new(workloadManager, qualificationArchive);
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
				qualificationWorkspaceState,
				profileCatalog));
	}
}
