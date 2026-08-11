// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Reporting;
using Singularity.Hardware.Models;

namespace Singularity.Application;

public sealed class ReportExportService
{
	private readonly QualificationJsonExporter jsonExporter = new();
	private readonly QualificationHtmlExporter htmlExporter = new();

	public void ExportJson(
		string path,
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion) =>
		jsonExporter.Export(path, report, hardware, singularityVersion);

	public void ExportHtml(
		string path,
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion) =>
		htmlExporter.Export(path, report, hardware, singularityVersion);
}

