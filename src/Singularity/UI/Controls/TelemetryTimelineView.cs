// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Drawing.Drawing2D;
using Singularity.Core.Reporting;

namespace Singularity.UI.Controls;

public sealed class TelemetryTimelineView : Panel
{
	private readonly FlowLayoutPanel charts = new();
	private QualificationTelemetryTimeline? timeline;

	public TelemetryTimelineView()
	{
		Dock = DockStyle.Top;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		BackColor = Theme.Panel;
		Padding = new Padding(ThemeMetrics.Spacing);

		Label title = new()
		{
			Dock = DockStyle.Top,
			Height = 32,
			Text = "TELEMETRY TIMELINE",
			Font = ThemeFonts.Header,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel
		};

		charts.Dock = DockStyle.Top;
		charts.AutoSize = true;
		charts.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		charts.FlowDirection = FlowDirection.TopDown;
		charts.WrapContents = false;
		charts.BackColor = Theme.Panel;

		Controls.Add(charts);
		Controls.Add(title);
		SizeChanged += (_, _) => UpdateChartWidths();
		Visible = false;
	}

	public void UpdateTimeline(QualificationTelemetryTimeline value)
	{
		ArgumentNullException.ThrowIfNull(value);
		if (ReferenceEquals(timeline, value))
			return;

		timeline = value;
		charts.SuspendLayout();
		try
		{
			charts.Controls.Clear();
			Visible = value.Points.Count >= 2;
			if (!Visible)
				return;

			charts.Controls.Add(new TimelineChartControl(
				"System utilization",
				"%",
				value,
				TimelineMetric.SystemUtilization));

			bool hasGpuTimeline = value.Points.Any(point => point.Gpus.Count > 0);
			if (hasGpuTimeline)
			{
				charts.Controls.Add(new TimelineChartControl("GPU load", "%", value, TimelineMetric.GpuLoad));
				charts.Controls.Add(new TimelineChartControl("GPU temperature", "°C", value, TimelineMetric.GpuTemperature));
				charts.Controls.Add(new TimelineChartControl("GPU power", "W", value, TimelineMetric.GpuPower));
				charts.Controls.Add(new TimelineChartControl("GPU VRAM usage", "%", value, TimelineMetric.GpuVram));
			}
		}
		finally
		{
			charts.ResumeLayout();
		}

		UpdateChartWidths();
	}

	private void UpdateChartWidths()
	{
		int width = Math.Max(
			320,
			ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
		foreach (Control chart in charts.Controls)
			chart.Width = width;
	}

	private enum TimelineMetric
	{
		SystemUtilization,
		GpuLoad,
		GpuTemperature,
		GpuPower,
		GpuVram
	}

	private sealed class TimelineChartControl : Control
	{
		private static readonly Color[] SeriesColors =
		[
			Theme.Accent,
			Theme.Success,
			Theme.Danger,
			Theme.TextMain,
			Theme.TextMuted
		];

		private readonly string title;
		private readonly string unit;
		private readonly QualificationTelemetryTimeline timeline;
		private readonly TimelineMetric metric;

		public TimelineChartControl(
			string title,
			string unit,
			QualificationTelemetryTimeline timeline,
			TimelineMetric metric)
		{
			this.title = title;
			this.unit = unit;
			this.timeline = timeline;
			this.metric = metric;
			Height = 176;
			Margin = new Padding(0, 0, 0, ThemeMetrics.SpacingSmall);
			BackColor = Theme.PanelLight;
			SetStyle(
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.ResizeRedraw |
				ControlStyles.UserPaint,
				true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			Rectangle bounds = ClientRectangle;
			if (bounds.Width < 120 || bounds.Height < 100)
				return;

			using Brush titleBrush = new SolidBrush(Theme.TextMain);
			using Brush mutedBrush = new SolidBrush(Theme.TextMuted);
			graphics.DrawString(title, ThemeFonts.CardTitle, titleBrush, 12, 8);

			List<Series> series = CreateSeries();
			double[] values = series
				.SelectMany(item => timeline.Points.Select(item.Selector))
				.Where(value => value.HasValue && double.IsFinite(value.Value))
				.Select(value => value!.Value)
				.ToArray();

			if (values.Length == 0)
			{
				graphics.DrawString("Unavailable", ThemeFonts.CardTextSmall, mutedBrush, 12, 42);
				return;
			}

			const float left = 48;
			const float top = 44;
			const float right = 14;
			const float bottom = 24;
			RectangleF plot = new(
				left,
				top,
				Math.Max(1, bounds.Width - left - right),
				Math.Max(1, bounds.Height - top - bottom));

			using Pen axisPen = new(Theme.Separator);
			graphics.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
			graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

			double yMaximum = IsPercentMetric(metric)
				? 100
				: Math.Max(1, Math.Ceiling(values.Max() * 1.1 / 10) * 10);
			double xMaximum = Math.Max(1, timeline.Points[^1].Elapsed.TotalSeconds);

			graphics.DrawString($"{yMaximum:0.#} {unit}", ThemeFonts.CardTextSmall, mutedBrush, 4, plot.Top - 4);
			graphics.DrawString("0", ThemeFonts.CardTextSmall, mutedBrush, 22, plot.Bottom - 14);
			graphics.DrawString("0:00", ThemeFonts.CardTextSmall, mutedBrush, plot.Left, plot.Bottom + 4);
			string elapsedLabel = FormatElapsed(timeline.Points[^1].Elapsed);
			SizeF elapsedSize = graphics.MeasureString(elapsedLabel, ThemeFonts.CardTextSmall);
			graphics.DrawString(
				elapsedLabel,
				ThemeFonts.CardTextSmall,
				mutedBrush,
				plot.Right - elapsedSize.Width,
				plot.Bottom + 4);

			DrawEvents(graphics, plot, xMaximum);
			DrawSeries(graphics, plot, xMaximum, yMaximum, series);
			DrawLegend(graphics, series);
		}

		private List<Series> CreateSeries()
		{
			if (metric == TimelineMetric.SystemUtilization)
			{
				return
				[
					new Series("CPU", SeriesColors[0], point => point.CpuLoadPercent),
					new Series("RAM", SeriesColors[1], point => point.SystemMemoryUsagePercent)
				];
			}

			string[] identifiers = timeline.Points
				.SelectMany(point => point.Gpus)
				.Select(gpu => gpu.Identifier)
				.Where(identifier => !string.IsNullOrWhiteSpace(identifier))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			List<Series> result = [];
			for (int index = 0; index < identifiers.Length; index++)
			{
				string identifier = identifiers[index];
				string name = timeline.Points
					.SelectMany(point => point.Gpus)
					.FirstOrDefault(gpu =>
						string.Equals(gpu.Identifier, identifier, StringComparison.OrdinalIgnoreCase) &&
						!string.IsNullOrWhiteSpace(gpu.Name))
					?.Name ?? identifier;
				Color color = SeriesColors[index % SeriesColors.Length];

				result.Add(new Series(
					name,
					color,
					point =>
					{
						QualificationTelemetryGpuPoint? gpu = point.Gpus.FirstOrDefault(item =>
							string.Equals(item.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
						return gpu is null ? null : SelectGpuMetric(gpu);
					}));
			}

			return result;
		}

		private double? SelectGpuMetric(QualificationTelemetryGpuPoint gpu) =>
			metric switch
			{
				TimelineMetric.GpuLoad => gpu.LoadPercent,
				TimelineMetric.GpuTemperature => gpu.TemperatureCelsius,
				TimelineMetric.GpuPower => gpu.PowerWatts,
				TimelineMetric.GpuVram => gpu.VramUsagePercent,
				_ => null
			};

		private void DrawEvents(Graphics graphics, RectangleF plot, double xMaximum)
		{
			using Pen eventPen = new(Theme.TextMuted) { DashStyle = DashStyle.Dash };
			foreach (QualificationTimelineEvent marker in timeline.Events)
			{
				float x = plot.Left + (float)Math.Clamp(marker.Elapsed.TotalSeconds / xMaximum, 0, 1) * plot.Width;
				graphics.DrawLine(eventPen, x, plot.Top, x, plot.Bottom);
			}
		}

		private void DrawSeries(
			Graphics graphics,
			RectangleF plot,
			double xMaximum,
			double yMaximum,
			IReadOnlyList<Series> series)
		{
			foreach (Series item in series)
			{
				using Pen pen = new(item.Color, 2);
				List<PointF> segment = [];
				foreach (QualificationTelemetryPoint point in timeline.Points)
				{
					double? value = item.Selector(point);
					if (value is null || !double.IsFinite(value.Value))
					{
						DrawSegment(graphics, pen, segment);
						segment.Clear();
						continue;
					}

					float x = plot.Left + (float)Math.Clamp(point.Elapsed.TotalSeconds / xMaximum, 0, 1) * plot.Width;
					float y = plot.Bottom - (float)Math.Clamp(value.Value / yMaximum, 0, 1) * plot.Height;
					segment.Add(new PointF(x, y));
				}
				DrawSegment(graphics, pen, segment);
			}
		}

		private void DrawLegend(Graphics graphics, IReadOnlyList<Series> series)
		{
			float x = 148;
			foreach (Series item in series)
			{
				using Brush swatch = new SolidBrush(item.Color);
				using Brush text = new SolidBrush(Theme.TextMuted);
				graphics.FillRectangle(swatch, x, 18, 14, 3);
				x += 20;
				graphics.DrawString(item.Name, ThemeFonts.CardTextSmall, text, x, 10);
				x += graphics.MeasureString(item.Name, ThemeFonts.CardTextSmall).Width + 14;
				if (x > Width - 120)
					break;
			}
		}

		private static void DrawSegment(Graphics graphics, Pen pen, IReadOnlyList<PointF> points)
		{
			if (points.Count >= 2)
				graphics.DrawLines(pen, points.ToArray());
		}

		private static bool IsPercentMetric(TimelineMetric value) =>
			value is TimelineMetric.SystemUtilization or TimelineMetric.GpuLoad or TimelineMetric.GpuVram;

		private static string FormatElapsed(TimeSpan elapsed) =>
			elapsed.TotalHours >= 1
				? elapsed.ToString(@"h\:mm\:ss")
				: elapsed.ToString(@"m\:ss");

		private sealed record Series(
			string Name,
			Color Color,
			Func<QualificationTelemetryPoint, double?> Selector);
	}
}
