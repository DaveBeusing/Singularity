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
	private readonly int contentWidth;

	public HistorySection(int width = LayoutConstants.MetricsPanelWidth, int height = 220)
	{
		Width = width;
		Height = height;
		contentWidth = width - LayoutConstants.SectionPadding * 2;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"HISTORY");

		listPanel = new FlowLayoutPanel
		{
			Left = LayoutConstants.SectionPadding,
			Top = 55,
			Width = contentWidth,
			Height = height - LayoutConstants.SectionHeaderHeight - LayoutConstants.SectionPadding,
			BackColor = Theme.Panel,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true
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

	private Label CreateEmptyLabel()
	{
		return new Label
		{
			Text = "No sessions yet",
			Width = contentWidth,
			Height = 26,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private Panel CreateHistoryRow(QualificationRecord record)
	{
		Panel row = new()
		{
			Width = contentWidth,
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
			ForeColor = StatusStyle.GetColor(record.Result),
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
			Left = contentWidth - 120,
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

}
