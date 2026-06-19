// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class HistorySection : Panel
{
	private readonly FlowLayoutPanel listPanel;

	public HistorySection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 220;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"HISTORY");

		listPanel = new FlowLayoutPanel
		{
			Left = 20,
			Top = 55,
			Width = 365,
			Height = 145,
			BackColor = Theme.Panel,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = false
		};

		Controls.Add(listPanel);
	}

	public void UpdateHistory(QualificationHistory history)
	{
		listPanel.Controls.Clear();

		if (history.Records.Count == 0)
		{
			listPanel.Controls.Add(CreateEmptyLabel());
			return;
		}

		foreach (QualificationRecord record in history.Records)
		{
			listPanel.Controls.Add(
				CreateHistoryRow(record));
		}
	}

	private static Label CreateEmptyLabel()
	{
		return new Label
		{
			Text = "No sessions yet",
			Width = 365,
			Height = 26,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private static Panel CreateHistoryRow(QualificationRecord record)
	{
		Panel row = new()
		{
			Width = 365,
			Height = 28,
			BackColor = Theme.PanelLight,
			Margin = new Padding(0, 0, 0, 6)
		};

		Label resultLabel = new()
		{
			Text = record.DisplayResult,
			Left = 10,
			Top = 0,
			Width = 90,
			Height = 28,
			Font = ThemeFonts.CardTitle,
			ForeColor = GetStatusColor(record.Result),
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label durationLabel = new()
		{
			Text = record.DisplayDuration,
			Left = 115,
			Top = 0,
			Width = 90,
			Height = 28,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label startedLabel = new()
		{
			Text = record.DisplayStarted,
			Left = 245,
			Top = 0,
			Width = 100,
			Height = 28,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleRight
		};

		row.Controls.AddRange([
			resultLabel,
			durationLabel,
			startedLabel
		]);

		return row;
	}

	private static Color GetStatusColor(ValidationStatus status)
	{
		return status switch
		{
			ValidationStatus.Pass => Theme.Success,
			ValidationStatus.Warning => Theme.Accent,
			ValidationStatus.Fail => Theme.Danger,
			_ => Theme.TextMuted
		};
	}

}