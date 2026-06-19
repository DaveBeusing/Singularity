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
	private readonly Label startedValue;
	private readonly Label durationValue;
	private readonly Label resultValue;

	public SessionSection()
	{
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 145;
		BackColor = Theme.Panel;

		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Metrics,
			"SESSION");

		Controls.Add(CreateTitleLabel("Started", 55));
		Controls.Add(CreateTitleLabel("Duration", 85));
		Controls.Add(CreateTitleLabel("Result", 115));

		startedValue = CreateValueLabel("Not Started", 55);
		durationValue = CreateValueLabel("00:00:00", 85);
		resultValue = CreateValueLabel("UNKNOWN", 115);

		Controls.Add(startedValue);
		Controls.Add(durationValue);
		Controls.Add(resultValue);
	}

	public void UpdateSession(QualificationSession session)
	{
		startedValue.Text =
			session.StartTime?.ToString("HH:mm:ss") ?? "Not Started";

		durationValue.Text =
			session.Duration.ToString(@"hh\:mm\:ss");

		resultValue.Text =
			session.Result.ToString().ToUpperInvariant();

		resultValue.ForeColor =
			session.Result switch
			{
				ValidationStatus.Pass => Theme.Success,
				ValidationStatus.Warning => Theme.Accent,
				ValidationStatus.Fail => Theme.Danger,
				_ => Theme.TextMuted
			};
	}

	private static Label CreateTitleLabel(string text, int top)
	{
		return new Label
		{
			Text = text,
			Left = 20,
			Top = top,
			Width = 90,
			Height = 20,
			Font = ThemeFonts.CardTitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel
		};
	}

	private static Label CreateValueLabel(string text, int top)
	{
		return new Label
		{
			Text = text,
			Left = 140,
			Top = top,
			Width = 220,
			Height = 20,
			Font = ThemeFonts.CardText,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Panel,
			TextAlign = ContentAlignment.MiddleRight
		};
	}

}