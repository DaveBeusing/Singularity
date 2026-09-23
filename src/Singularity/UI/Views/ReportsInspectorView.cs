// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Qualification;

namespace Singularity.UI.Views;

public sealed class ReportsInspectorView : Panel
{
	private readonly Label titleLabel = new();
	private readonly Label bodyLabel = new();

	public ReportsInspectorView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Inspector;
		Padding = new Padding(ThemeMetrics.Spacing);

		titleLabel.Dock = DockStyle.Top;
		titleLabel.Height = 34;
		titleLabel.Text = "REPORT DETAILS";
		titleLabel.Font = ThemeFonts.Header;
		titleLabel.ForeColor = Theme.TextMain;
		titleLabel.BackColor = Theme.Inspector;

		bodyLabel.Dock = DockStyle.Fill;
		bodyLabel.Font = ThemeFonts.CardText;
		bodyLabel.ForeColor = Theme.TextMuted;
		bodyLabel.BackColor = Theme.Inspector;
		bodyLabel.TextAlign = ContentAlignment.TopLeft;

		Controls.Add(bodyLabel);
		Controls.Add(titleLabel);
		UpdateState(ReportsWorkspaceSnapshot.Empty);
	}

	public void UpdateState(ReportsWorkspaceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);

		if (snapshot.SelectedRecord is not { } record)
		{
			bodyLabel.Text = "No qualification history entry is selected.";
			return;
		}

		string mode = record.ExecutionMode == QualificationExecutionMode.Unknown
			? "Unavailable"
			: record.ExecutionMode.ToString();

		bodyLabel.Text =
			$"Result\r\n{StatusStyle.Format(record.Result)}\r\n\r\n" +
			$"Profile\r\n{record.ProfileName}\r\n\r\n" +
			$"Mode\r\n{mode}\r\n\r\n" +
			$"Started\r\n{record.StartedAt:G}\r\n\r\n" +
			$"Finished\r\n{record.FinishedAt:G}\r\n\r\n" +
			$"Duration\r\n{record.Duration:hh\\:mm\\:ss}\r\n\r\n" +
			$"GPU evidence\r\n{record.GpuEvidence.Count} device(s)\r\n\r\n" +
			$"Export evidence\r\n{(snapshot.SelectedReport is null ? "Unavailable" : "JSON / HTML available")}";
	}
}
