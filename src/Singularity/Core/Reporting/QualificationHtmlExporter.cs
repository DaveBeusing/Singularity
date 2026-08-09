// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Text;
using Singularity.Core.Validation;
using Singularity.Hardware.Models;

namespace Singularity.Core.Reporting;

public sealed class QualificationHtmlExporter
{
	public string Render(
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion)
	{
		QualificationJsonDocument document = QualificationJsonExporter.CreateDocument(
			report, hardware, singularityVersion);
		StringBuilder html = new(8192);
		html.Append("""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Singularity Qualification Report</title>
<style>
:root{color-scheme:dark;--bg:#101217;--panel:#191d25;--line:#2b3240;--text:#f1f4f8;--muted:#929bad;--accent:#f1bf42;--success:#38c878;--danger:#e45b66}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 "Segoe UI",system-ui,sans-serif}.page{width:min(1080px,calc(100% - 32px));margin:32px auto 56px}header{border-bottom:2px solid var(--accent);padding:0 0 20px}h1{margin:0;font-size:32px;letter-spacing:.02em}h1 span{color:var(--accent)}.subtitle,.muted{color:var(--muted)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-top:16px}.card{background:var(--panel);border:1px solid var(--line);padding:20px}.card.wide{grid-column:1/-1}h2{margin:0 0 14px;color:var(--accent);font-size:14px;letter-spacing:.12em}dl{display:grid;grid-template-columns:minmax(120px,1fr) 2fr;gap:8px 16px;margin:0}dt{color:var(--muted)}dd{margin:0;overflow-wrap:anywhere}.results{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}.result{padding:14px;text-align:center;background:#11141a;border-top:3px solid var(--muted)}.result.pass{border-color:var(--success)}.result.warning{border-color:var(--accent)}.result.fail{border-color:var(--danger)}.result strong{display:block;font-size:18px}table{width:100%;border-collapse:collapse}th,td{padding:9px 10px;border-bottom:1px solid var(--line);text-align:right}th:first-child,td:first-child{text-align:left}th{color:var(--muted);font-weight:600}.list{margin:0;padding-left:20px}.footer{margin-top:20px;color:var(--muted);font-size:12px}@media(max-width:720px){.grid{grid-template-columns:1fr}.results{grid-template-columns:repeat(2,1fr)}dl{grid-template-columns:1fr}.card{padding:16px}}
</style>
</head>
<body><main class="page">
""");
		html.Append("<header><h1>//Singularity<span>✦</span></h1><div class=\"subtitle\">Platform Qualification Report · ")
			.Append(H(document.SingularityVersion)).Append("</div></header>");
		AppendSession(html, document);
		AppendValidation(html, document.Validation);
		AppendTelemetry(html, document.TelemetryStatistics);
		AppendHardware(html, document.Hardware);
		html.Append("<div class=\"footer\">Schema ").Append(H(document.SchemaVersion))
			.Append(" · Generated ").Append(H(document.Timestamp.ToString("O", CultureInfo.InvariantCulture)))
			.Append("</div></main></body></html>");
		return html.ToString();
	}

	public void Export(string path, QualificationReport report, HardwareInventory hardware, string singularityVersion)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		File.WriteAllText(path, Render(report, hardware, singularityVersion));
	}

	private static void AppendSession(StringBuilder html, QualificationJsonDocument document)
	{
		QualificationProfile profile = document.QualificationProfile;
		html.Append("<div class=\"grid\"><section class=\"card\"><h2>SESSION</h2><dl>")
			.Append(Row("Timestamp", document.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)))
			.Append(Row("Duration", document.SessionDuration.ToString(@"hh\:mm\:ss")))
			.Append(Row("Profile", profile.Name)).Append("</dl></section>")
			.Append("<section class=\"card\"><h2>PROFILE</h2><dl>")
			.Append(Row("CPU minimum", $"{profile.CpuMinimumLoadPercent:0}%"))
			.Append(Row("Memory tolerance", $"{profile.MemoryAllocationTolerancePercent:0}%"))
			.Append(Row("GPU minimum", $"{profile.GpuMinimumLoadPercent:0}%"))
			.Append(Row("GPU maximum temp", $"{profile.GpuMaximumTemperatureCelsius:0} °C"))
			.Append("</dl></section></div>");
	}

	private static void AppendValidation(StringBuilder html, QualificationValidationJson validation)
	{
		html.Append("<section class=\"card wide\" style=\"margin-top:16px\"><h2>VALIDATION</h2><div class=\"results\">");
		AppendResult(html, "CPU", validation.Cpu);
		AppendResult(html, "MEMORY", validation.Memory);
		AppendResult(html, "GPU", validation.Gpu);
		AppendResult(html, "OVERALL", validation.Overall);
		html.Append("</div></section>");
	}

	private static void AppendResult(StringBuilder html, string name, ValidationStatus status)
	{
		string value = status.ToString().ToUpperInvariant();
		html.Append("<div class=\"result ").Append(value.ToLowerInvariant()).Append("\"><span class=\"muted\">")
			.Append(name).Append("</span><strong>").Append(value).Append("</strong></div>");
	}

	private static void AppendTelemetry(StringBuilder html, SessionTelemetryStatistics statistics)
	{
		html.Append("<section class=\"card wide\" style=\"margin-top:16px\"><h2>TELEMETRY</h2><table><thead><tr><th>Metric</th><th>Min</th><th>Average</th><th>Max</th><th>Samples</th></tr></thead><tbody>");
		AppendMetric(html, "CPU load", statistics.CpuLoadPercent, "%");
		AppendMetric(html, "GPU load", statistics.GpuLoadPercent, "%");
		AppendMetric(html, "GPU temperature", statistics.GpuTemperatureCelsius, "°C");
		AppendMetric(html, "GPU power", statistics.GpuPowerWatts, "W");
		AppendMetric(html, "GPU VRAM usage", statistics.GpuVramUsagePercent, "%");
		AppendMetric(html, "System memory usage", statistics.SystemMemoryUsagePercent, "%");
		html.Append("</tbody></table></section>");
	}

	private static void AppendMetric(StringBuilder html, string name, MetricStatistics? metric, string unit)
	{
		if (metric is null)
		{
			html.Append("<tr><td>").Append(H(name)).Append("</td><td colspan=\"4\">Unavailable</td></tr>");
			return;
		}
		html.Append("<tr><td>").Append(H(name)).Append("</td><td>").Append(Number(metric.Minimum, unit))
			.Append("</td><td>").Append(Number(metric.Average, unit)).Append("</td><td>")
			.Append(Number(metric.Maximum, unit)).Append("</td><td>").Append(metric.SampleCount).Append("</td></tr>");
	}

	private static void AppendHardware(StringBuilder html, HardwareSummaryJson hardware)
	{
		html.Append("<div class=\"grid\"><section class=\"card\"><h2>MACHINE</h2><dl>")
			.Append(Row("Computer", hardware.ComputerName)).Append(Row("Operating system", hardware.OperatingSystem))
			.Append(Row("Mainboard", hardware.Mainboard)).Append(Row("Processor", hardware.Processor))
			.Append(Row("CPU topology", $"{hardware.ProcessorCores} cores / {hardware.ProcessorThreads} threads"))
			.Append("</dl></section><section class=\"card\"><h2>DEVICES</h2><ul class=\"list\">");
		foreach (GpuSummaryJson gpu in hardware.Gpus)
			html.Append("<li>").Append(H($"{gpu.Name} · {gpu.Vram} · {gpu.PcieLink}")).Append("</li>");
		foreach (string memory in hardware.MemoryModules)
			html.Append("<li>").Append(H(memory)).Append("</li>");
		foreach (string storage in hardware.StorageDrives)
			html.Append("<li>").Append(H(storage)).Append("</li>");
		html.Append("</ul></section></div>");
	}

	private static string Row(string name, string value) => $"<dt>{H(name)}</dt><dd>{H(value)}</dd>";
	private static string Number(double value, string unit) => $"{value.ToString("0.0", CultureInfo.InvariantCulture)} {H(unit)}";
	private static string H(string value) => WebUtility.HtmlEncode(value);
}
