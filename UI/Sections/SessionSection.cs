// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class SessionSection : Panel
{
	private readonly ValueRow startedRow;
	private readonly ValueRow durationRow;
	private readonly ValueRow resultRow;

	public SessionSection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 145;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"SESSION");

		startedRow = CreateRow("Started", "Not Started", 55);
		durationRow = CreateRow("Duration", "00:00:00", 85);
		resultRow = CreateRow("Result", "UNKNOWN", 115);
		Controls.AddRange([startedRow, durationRow, resultRow]);
	}

	public void UpdateSession(QualificationSession session)
	{
		startedRow.SetValue(session.StartTime?.ToString("HH:mm:ss") ?? "Not Started");
		durationRow.SetValue(session.Duration.ToString(@"hh\:mm\:ss"));
		resultRow.SetValue(StatusStyle.Format(session.Result), StatusStyle.GetColor(session.Result));
	}

	private static ValueRow CreateRow(string title, string value, int top)
	{
		return new ValueRow(title, value)
		{
			Left = LayoutConstants.SectionPadding,
			Top = top
		};
	}

}
