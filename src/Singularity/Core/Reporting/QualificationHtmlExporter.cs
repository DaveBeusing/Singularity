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
	private static readonly string[] TimelinePalette =
	[
		"#f1bf42",
		"#38c878",
		"#e45b66",
		"#8f9cff",
		"#55c7d9",
		"#d78ee8"
	];
	public string Render(
		QualificationReport report,
		HardwareInventory hardware,
		string singularityVersion)
	{
		QualificationJsonDocument document = QualificationJsonExporter.CreateDocument(
			report, hardware, singularityVersion);
		StringBuilder html = new(12288);
		html.Append("""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Singularity Qualification Report</title>
<style>
:root{color-scheme:dark;--bg:#101217;--panel:#191d25;--line:#2b3240;--text:#f1f4f8;--muted:#929bad;--accent:#f1bf42;--success:#38c878;--danger:#e45b66}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 "Segoe UI",system-ui,sans-serif}.page{width:min(1080px,calc(100% - 32px));margin:32px auto 56px}header{border-bottom:2px solid var(--accent);padding:0 0 20px}h1{margin:0;font-size:32px;letter-spacing:.02em}h1 span{color:var(--accent)}.subtitle,.muted{color:var(--muted)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-top:16px}.card{background:var(--panel);border:1px solid var(--line);padding:20px}.card.wide{grid-column:1/-1}h2{margin:0 0 14px;color:var(--accent);font-size:14px;letter-spacing:.12em}h3{margin:0 0 8px;font-size:16px}dl{display:grid;grid-template-columns:minmax(120px,1fr) 2fr;gap:8px 16px;margin:0}dt{color:var(--muted)}dd{margin:0;overflow-wrap:anywhere}.results{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}.result{padding:14px;text-align:center;background:#11141a;border-top:3px solid var(--muted)}.result.pass{border-color:var(--success)}.result.warning{border-color:var(--accent)}.result.fail{border-color:var(--danger)}.result strong{display:block;font-size:18px}.gpu-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.gpu-card{background:#11141a;border:1px solid var(--line);padding:14px}.gpu-card.pass{border-top:3px solid var(--success)}.gpu-card.warning{border-top:3px solid var(--accent)}.gpu-card.fail{border-top:3px solid var(--danger)}.identifier{font:12px/1.4 Consolas,monospace;color:var(--muted);overflow-wrap:anywhere;margin-bottom:10px}table{width:100%;border-collapse:collapse}th,td{padding:9px 10px;border-bottom:1px solid var(--line);text-align:right}th:first-child,td:first-child{text-align:left}th{color:var(--muted);font-weight:600}.list{margin:0;padding-left:20px}.footer{margin-top:20px;color:var(--muted);font-size:12px}@media(max-width:720px){.grid,.gpu-grid{grid-template-columns:1fr}.results{grid-template-columns:repeat(2,1fr)}dl{grid-template-columns:1fr}.card{padding:16px}}
.timeline-grid{display:grid;grid-template-columns:1fr;gap:14px}.timeline-chart{background:#11141a;border:1px solid var(--line);padding:12px}.timeline-chart h3{margin-bottom:4px}.timeline-chart svg{display:block;width:100%;height:auto}.timeline-legend{display:flex;flex-wrap:wrap;gap:12px;margin:6px 0 2px;color:var(--muted);font-size:12px}.timeline-swatch{display:inline-block;width:16px;height:3px;margin-right:6px;vertical-align:middle}.timeline-axis{stroke:#3b4353;stroke-width:1}.timeline-event{stroke:#667085;stroke-width:1;stroke-dasharray:4 4}.timeline-label{fill:#929bad;font:11px "Segoe UI",sans-serif}
</style>
</head>
<body><main class="page">
""");
		html.Append("<header><h1>//Singularity<span>✦</span></h1><div class='subtitle'>Platform Qualification Report · ")
			.Append(H(document.SingularityVersion)).Append("</div></header>");
		AppendSession(html, document);
		AppendValidation(html, document.Validation);
		AppendGpuEvidence(html, document.GpuEvidence);
		AppendTelemetry(html, document.TelemetryStatistics);
		AppendTimeline(html, document.TelemetryTimeline);
		AppendHardware(html, document.Hardware);
		html.Append("<div class='footer'>Schema ").Append(H(document.SchemaVersion))
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
		html.Append("<div class='grid'><section class='card'><h2>SESSION</h2><dl>")
			.Append(Row("Timestamp", document.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)))
			.Append(Row("Duration", document.SessionDuration.ToString("c", CultureInfo.InvariantCulture)))
			.Append(Row("Profile", profile.Name))
			.Append(Row("Profile identity", profile.Id))
			.Append(Row("Profile origin", profile.Origin.ToString()))
			.Append("</dl></section>")
			.Append("<section class='card'><h2>PROFILE</h2><dl>")
			.Append(Row("Recommended duration", profile.RecommendedDuration.ToString("c", CultureInfo.InvariantCulture)))
			.Append(Row("CPU minimum", $"{profile.CpuMinimumLoadPercent:0}%"))
			.Append(Row("CPU warning", $"{profile.CpuWarningLoadPercent:0}%"))
			.Append(Row("Memory tolerance", $"{profile.MemoryAllocationTolerancePercent:0}%"))
			.Append(Row("Memory warning", $"{profile.MemoryWarningTolerancePercent:0}%"))
			.Append(Row("GPU minimum", $"{profile.GpuMinimumLoadPercent:0}%"))
			.Append(Row("GPU maximum temp", $"{profile.GpuMaximumTemperatureCelsius:0} °C"))
			.Append(Row("GPU warm-up", profile.GpuWarmupDuration.ToString("c", CultureInfo.InvariantCulture)))
			.Append(Row("GPU stability", profile.GpuStabilityDuration.ToString("c", CultureInfo.InvariantCulture)))
			.Append("</dl></section></div>");
	}

	private static void AppendValidation(StringBuilder html, QualificationValidationJson validation)
	{
		html.Append("<section class='card wide' style='margin-top:16px'><h2>VALIDATION</h2><div class='results'>");
		AppendResult(html, "CPU", validation.Cpu);
		AppendResult(html, "MEMORY", validation.Memory);
		AppendResult(html, "GPU", validation.Gpu);
		AppendResult(html, "OVERALL", validation.Overall);
		html.Append("</div></section>");
	}

	private static void AppendResult(StringBuilder html, string name, ValidationStatus status)
	{
		string value = status.ToString().ToUpperInvariant();
		html.Append("<div class='result ").Append(value.ToLowerInvariant()).Append("'><span class='muted'>")
			.Append(name).Append("</span><strong>").Append(value).Append("</strong></div>");
	}

	private static void AppendGpuEvidence(
		StringBuilder html,
		IReadOnlyList<GpuQualificationEvidenceJson> evidence)
	{
		if (evidence.Count == 0)
			return;

		html.Append("<section class='card wide' style='margin-top:16px'><h2>GPU DEVICE EVIDENCE</h2><div class='gpu-grid'>");
		foreach (GpuQualificationEvidenceJson gpu in evidence)
		{
			string status = gpu.Validation.ToString().ToLowerInvariant();
			html.Append("<article class='gpu-card ").Append(status).Append("'><h3>")
				.Append(H(gpu.Name)).Append("</h3><div class='identifier'>")
				.Append(H(gpu.Identifier)).Append("</div><dl>")
				.Append(Row("Validation", gpu.Validation.ToString().ToUpperInvariant()))
				.Append(Row("Message", gpu.ValidationMessage))
				.Append(Row("Telemetry", gpu.TelemetryAvailable ? "Available" : "Unavailable"))
				.Append(Row("Available samples", gpu.Telemetry.AvailableSampleCount.ToString(CultureInfo.InvariantCulture)))
				.Append(Row("Unavailable samples", gpu.Telemetry.UnavailableSampleCount.ToString(CultureInfo.InvariantCulture)))
				.Append(Row("Load avg / max", MetricAverageMaximum(gpu.Telemetry.LoadPercent, "%")))
				.Append(Row("Temperature avg / max", MetricAverageMaximum(gpu.Telemetry.TemperatureCelsius, "°C")))
				.Append(Row("Power avg / max", MetricAverageMaximum(gpu.Telemetry.PowerWatts, "W")))
				.Append(Row("VRAM avg / max", MetricAverageMaximum(gpu.Telemetry.VramUsagePercent, "%")))
				.Append("</dl></article>");
		}

		html.Append("</div></section>");
	}

	private static void AppendTelemetry(StringBuilder html, SessionTelemetryStatistics statistics)
	{
		html.Append("<section class='card wide' style='margin-top:16px'><h2>SESSION TELEMETRY</h2><table><thead><tr><th>Metric</th><th>Min</th><th>Average</th><th>Max</th><th>Samples</th></tr></thead><tbody>");
		AppendMetric(html, "CPU load", statistics.CpuLoadPercent, "%");
		AppendMetric(html, "GPU load (single-GPU compatibility)", statistics.GpuLoadPercent, "%");
		AppendMetric(html, "GPU temperature (single-GPU compatibility)", statistics.GpuTemperatureCelsius, "°C");
		AppendMetric(html, "GPU power (single-GPU compatibility)", statistics.GpuPowerWatts, "W");
		AppendMetric(html, "GPU VRAM usage (single-GPU compatibility)", statistics.GpuVramUsagePercent, "%");
		AppendMetric(html, "System memory usage", statistics.SystemMemoryUsagePercent, "%");
		html.Append("</tbody></table></section>");
	}

	private static void AppendMetric(StringBuilder html, string name, MetricStatistics? metric, string unit)
	{
		if (metric is null)
		{
			html.Append("<tr><td>").Append(H(name)).Append("</td><td colspan='4'>Unavailable</td></tr>");
			return;
		}

		html.Append("<tr><td>").Append(H(name)).Append("</td><td>").Append(Number(metric.Minimum, unit))
			.Append("</td><td>").Append(Number(metric.Average, unit)).Append("</td><td>")
			.Append(Number(metric.Maximum, unit)).Append("</td><td>").Append(metric.SampleCount).Append("</td></tr>");
	}

	private static void AppendTimeline(
		StringBuilder html,
		QualificationTelemetryTimeline timeline)
	{
		if (timeline.Points.Count < 2)
			return;

		html.Append("<section class='card wide' style='margin-top:16px'><h2>TELEMETRY TIMELINE</h2><div class='timeline-grid'>");

		AppendTimelineChart(
			html,
			"System utilization",
			"%",
			timeline,
			[
				new TimelineSeries("CPU load", TimelinePalette[0], point => point.CpuLoadPercent),
				new TimelineSeries("System memory", TimelinePalette[1], point => point.SystemMemoryUsagePercent)
			],
			100);

		string[] gpuIdentifiers = timeline.Points
			.SelectMany(point => point.Gpus)
			.Select(gpu => gpu.Identifier)
			.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (gpuIdentifiers.Length > 0)
		{
			AppendGpuTimelineChart(html, "GPU load", "%", timeline, gpuIdentifiers, gpu => gpu.LoadPercent, 100);
			AppendGpuTimelineChart(html, "GPU temperature", "°C", timeline, gpuIdentifiers, gpu => gpu.TemperatureCelsius, null);
			AppendGpuTimelineChart(html, "GPU power", "W", timeline, gpuIdentifiers, gpu => gpu.PowerWatts, null);
			AppendGpuTimelineChart(html, "GPU VRAM usage", "%", timeline, gpuIdentifiers, gpu => gpu.VramUsagePercent, 100);
		}

		html.Append("</div></section>");
	}

	private static void AppendGpuTimelineChart(
		StringBuilder html,
		string title,
		string unit,
		QualificationTelemetryTimeline timeline,
		IReadOnlyList<string> gpuIdentifiers,
		Func<QualificationTelemetryGpuPoint, double?> selector,
		double? fixedMaximum)
	{
		List<TimelineSeries> series = [];
		for (int index = 0; index < gpuIdentifiers.Count; index++)
		{
			string identifier = gpuIdentifiers[index];
			string name = timeline.Points
				.SelectMany(point => point.Gpus)
				.FirstOrDefault(gpu =>
					string.Equals(gpu.Identifier, identifier, StringComparison.OrdinalIgnoreCase) &&
					!string.IsNullOrWhiteSpace(gpu.Name))
				?.Name ?? identifier;
			string color = TimelinePalette[index % TimelinePalette.Length];

			series.Add(new TimelineSeries(
				name,
				color,
				point =>
				{
					QualificationTelemetryGpuPoint? gpu = point.Gpus.FirstOrDefault(item =>
						string.Equals(item.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
					return gpu is null ? null : selector(gpu);
				}));
		}

		AppendTimelineChart(html, title, unit, timeline, series, fixedMaximum);
	}

	private static void AppendTimelineChart(
		StringBuilder html,
		string title,
		string unit,
		QualificationTelemetryTimeline timeline,
		IReadOnlyList<TimelineSeries> series,
		double? fixedMaximum)
	{
		double[] values = series
			.SelectMany(item => timeline.Points.Select(item.Selector))
			.Where(value => value.HasValue && double.IsFinite(value.Value))
			.Select(value => value!.Value)
			.ToArray();
		if (values.Length == 0)
			return;

		const double width = 800;
		const double height = 200;
		const double left = 48;
		const double top = 12;
		const double right = 16;
		const double bottom = 30;
		double plotWidth = width - left - right;
		double plotHeight = height - top - bottom;
		double xMaximum = Math.Max(1, timeline.Points[^1].Elapsed.TotalSeconds);
		double yMaximum = fixedMaximum ?? Math.Max(1, Math.Ceiling(values.Max() * 1.1 / 10) * 10);

		html.Append("<article class='timeline-chart'><h3>").Append(H(title)).Append("</h3><div class='timeline-legend'>");
		foreach (TimelineSeries item in series)
		{
			html.Append("<span><i class='timeline-swatch' style='background:")
				.Append(item.Color)
				.Append("'></i>")
				.Append(H(item.Name))
				.Append("</span>");
		}
		html.Append("</div><svg viewBox='0 0 800 200' role='img' aria-label='")
			.Append(H(title))
			.Append(" timeline'>");
		html.Append("<line class='timeline-axis' x1='48' y1='12' x2='48' y2='170'/><line class='timeline-axis' x1='48' y1='170' x2='784' y2='170'/>");
		html.Append("<text class='timeline-label' x='4' y='20'>")
			.Append(H($"{yMaximum:0.#} {unit}"))
			.Append("</text><text class='timeline-label' x='22' y='168'>0</text>");
		html.Append("<text class='timeline-label' x='48' y='192'>0:00</text><text class='timeline-label' text-anchor='end' x='784' y='192'>")
			.Append(H(FormatElapsed(timeline.Points[^1].Elapsed)))
			.Append("</text>");

		foreach (QualificationTimelineEvent marker in timeline.Events)
		{
			double x = left + Math.Clamp(marker.Elapsed.TotalSeconds / xMaximum, 0, 1) * plotWidth;
			html.Append("<line class='timeline-event' x1='").Append(F(x)).Append("' y1='12' x2='")
				.Append(F(x)).Append("' y2='170'><title>")
				.Append(H($"{marker.Kind}: {marker.Label}"))
				.Append("</title></line>");
		}

		foreach (TimelineSeries item in series)
		{
			List<string> segment = [];
			foreach (QualificationTelemetryPoint point in timeline.Points)
			{
				double? value = item.Selector(point);
				if (value is null || !double.IsFinite(value.Value))
				{
					AppendSvgSegment(html, segment, item.Color);
					segment.Clear();
					continue;
				}

				double x = left + Math.Clamp(point.Elapsed.TotalSeconds / xMaximum, 0, 1) * plotWidth;
				double y = top + (1 - Math.Clamp(value.Value / yMaximum, 0, 1)) * plotHeight;
				segment.Add($"{F(x)},{F(y)}");
			}
			AppendSvgSegment(html, segment, item.Color);
		}

		html.Append("</svg></article>");
	}

	private static void AppendSvgSegment(StringBuilder html, IReadOnlyList<string> points, string color)
	{
		if (points.Count < 2)
			return;

		html.Append("<polyline fill='none' stroke='").Append(color)
			.Append("' stroke-width='2' vector-effect='non-scaling-stroke' points='")
			.Append(string.Join(' ', points))
			.Append("'/>");
	}

	private static string FormatElapsed(TimeSpan elapsed) =>
		elapsed.TotalHours >= 1
			? elapsed.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
			: elapsed.ToString(@"m\:ss", CultureInfo.InvariantCulture);

	private static string F(double value) =>
		value.ToString("0.##", CultureInfo.InvariantCulture);

	private sealed record TimelineSeries(
		string Name,
		string Color,
		Func<QualificationTelemetryPoint, double?> Selector);

	private static void AppendHardware(StringBuilder html, HardwareSummaryJson hardware)
	{
		html.Append("<div class='grid'><section class='card'><h2>MACHINE</h2><dl>")
			.Append(Row("Computer", hardware.ComputerName)).Append(Row("Operating system", hardware.OperatingSystem))
			.Append(Row("Mainboard", hardware.Mainboard)).Append(Row("Processor", hardware.Processor))
			.Append(Row("CPU topology", $"{hardware.ProcessorCores} cores / {hardware.ProcessorThreads} threads"))
			.Append("</dl></section><section class='card'><h2>DEVICES</h2><ul class='list'>");
		foreach (GpuSummaryJson gpu in hardware.Gpus)
			html.Append("<li>").Append(H($"{gpu.Name} · {gpu.Vram} · {gpu.PcieLink} · {gpu.Identifier}")).Append("</li>");
		foreach (string memory in hardware.MemoryModules)
			html.Append("<li>").Append(H(memory)).Append("</li>");
		foreach (string storage in hardware.StorageDrives)
			html.Append("<li>").Append(H(storage)).Append("</li>");
		html.Append("</ul></section></div>");
	}

	private static string MetricAverageMaximum(MetricStatistics? metric, string unit)
	{
		return metric is null
			? "Unavailable"
			: $"{Number(metric.Average, unit)} / {Number(metric.Maximum, unit)}";
	}

	private static string Row(string name, string value) => $"<dt>{H(name)}</dt><dd>{H(value)}</dd>";
	private static string Number(double value, string unit) => $"{value.ToString("0.0", CultureInfo.InvariantCulture)} {H(unit)}";
	private static string H(string value) => WebUtility.HtmlEncode(value);
}
