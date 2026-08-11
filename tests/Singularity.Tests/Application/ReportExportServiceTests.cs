// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Reporting;
using Singularity.Core.Validation;
using Singularity.Hardware.Models;

namespace Singularity.Tests.Application;

public sealed class ReportExportServiceTests
{
	[Fact]
	public void Export_WritesJsonAndHtmlReports()
	{
		ReportExportService service = new();
		QualificationReport report = new()
		{
			StartedAt = new DateTime(2026, 1, 2, 3, 4, 5),
			FinishedAt = new DateTime(2026, 1, 2, 3, 5, 5),
			Duration = TimeSpan.FromMinutes(1),
			Profile = QualificationProfiles.Quick,
			OverallResult = ValidationStatus.Pass
		};
		HardwareInventory hardware = new();
		string jsonPath = Path.Combine(Path.GetTempPath(), $"singularity-{Guid.NewGuid():N}.json");
		string htmlPath = Path.Combine(Path.GetTempPath(), $"singularity-{Guid.NewGuid():N}.html");

		try
		{
			service.ExportJson(jsonPath, report, hardware, "v-test");
			service.ExportHtml(htmlPath, report, hardware, "v-test");

			string json = File.ReadAllText(jsonPath);
			string html = File.ReadAllText(htmlPath);
			Assert.Contains("\"singularityVersion\": \"v-test\"", json);
			Assert.Contains("Singularity Qualification Report", html);
			Assert.Contains("v-test", html);
		}
		finally
		{
			File.Delete(jsonPath);
			File.Delete(htmlPath);
		}
	}
}

