// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;

namespace Singularity.UI.Views;

public sealed class QualificationInspectorView : Panel
{
	private readonly Label headingLabel = new();
	private readonly Label detailsLabel = new();

	public QualificationInspectorView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Inspector;
		Padding = new Padding(ThemeMetrics.Spacing);

		headingLabel.Dock = DockStyle.Top;
		headingLabel.Height = 34;
		headingLabel.Font = ThemeFonts.Header;
		headingLabel.ForeColor = Theme.TextMain;
		headingLabel.BackColor = Theme.Inspector;

		detailsLabel.Dock = DockStyle.Fill;
		detailsLabel.Font = ThemeFonts.CardText;
		detailsLabel.ForeColor = Theme.TextMuted;
		detailsLabel.BackColor = Theme.Inspector;
		detailsLabel.TextAlign = ContentAlignment.TopLeft;

		Controls.Add(detailsLabel);
		Controls.Add(headingLabel);
	}

	public void UpdateState(string? contextId, QualificationWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		switch (contextId)
		{
			case "workloads":
				ShowWorkloads(snapshot);
				break;
			case "session":
				ShowSession(snapshot);
				break;
			default:
				ShowProfile(snapshot);
				break;
		}
	}

	private void ShowProfile(QualificationWorkspaceSnapshot snapshot)
	{
		var profile = snapshot.Configuration.Profile;
		headingLabel.Text = "PROFILE";
		detailsLabel.Text =
			$"{profile.Name}\r\n\r\n" +
			$"Recommended duration: {profile.RecommendedDuration}\r\n" +
			$"CPU minimum: {profile.CpuMinimumLoadPercent:0}%\r\n" +
			$"CPU warning: {profile.CpuWarningLoadPercent:0}%\r\n" +
			$"Memory tolerance: {profile.MemoryAllocationTolerancePercent:0}%\r\n" +
			$"GPU minimum: {profile.GpuMinimumLoadPercent:0}%\r\n" +
			$"GPU maximum temperature: {profile.GpuMaximumTemperatureCelsius:0} °C";
	}

	private void ShowWorkloads(QualificationWorkspaceSnapshot snapshot)
	{
		QualificationConfiguration configuration = snapshot.Configuration;
		headingLabel.Text = "WORKLOADS";
		detailsLabel.Text =
			$"CPU: {FormatEnabled(configuration.EnableCpuWorkload)}\r\n" +
			$"Threads: {configuration.CpuThreads}\r\n\r\n" +
			$"Memory: {FormatEnabled(configuration.EnableMemoryWorkload)}\r\n" +
			$"Allocation: {configuration.MemoryGb} GB\r\n\r\n" +
			$"GPU: {FormatEnabled(configuration.EnableGpuWorkload)}\r\n" +
			$"Target load: {configuration.GpuLoadPercent}%\r\n\r\n" +
			$"Current workload state: {snapshot.WorkloadState}";
	}

	private void ShowSession(QualificationWorkspaceSnapshot snapshot)
	{
		headingLabel.Text = "SESSION";
		detailsLabel.Text =
			$"State: {snapshot.OverallState}\r\n" +
			$"Mode: {snapshot.Mode}\r\n" +
			$"Profile: {snapshot.Configuration.Profile.Name}\r\n" +
			$"Started: {snapshot.StartedAt?.ToString("G") ?? "Not started"}\r\n" +
			$"Elapsed: {snapshot.Elapsed:hh\\:mm\\:ss}\r\n" +
			$"Automated state: {snapshot.AutomatedState}\r\n" +
			$"Progress: {snapshot.ProgressPercent:0}%";
	}

	private static string FormatEnabled(bool enabled) => enabled ? "Enabled" : "Disabled";
}
