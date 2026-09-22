// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public enum StatusVisualState
{
	Neutral,
	Active,
	Success,
	Warning,
	Failure
}

public sealed class StatusIndicator : Label
{
	public StatusIndicator()
	{
		AutoSize = false;
		Height = ThemeMetrics.StatusBarHeight;
		Width = 120;
		TextAlign = ContentAlignment.MiddleCenter;
		Font = ThemeFonts.SectionHeader;
		SetState("READY", StatusVisualState.Neutral);
	}

	public void SetState(string text, StatusVisualState state)
	{
		ControlUpdate.SetText(this, text);

		Color backColor = state switch
		{
			StatusVisualState.Active => Theme.PrimaryAction,
			StatusVisualState.Success => Theme.Success,
			StatusVisualState.Warning => Theme.Warning,
			StatusVisualState.Failure => Theme.Failure,
			_ => Theme.StatusBar
		};

		Color foreColor = state is StatusVisualState.Active or StatusVisualState.Warning
			? Color.Black
			: Theme.TextMain;

		ControlUpdate.SetBackColor(this, backColor);
		ControlUpdate.SetForeColor(this, foreColor);
	}
}
