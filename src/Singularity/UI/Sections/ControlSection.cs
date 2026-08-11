// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;
using Singularity.UI.Factories;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class ControlSection : Panel
{
	public Button StartButton { get; } = new();
	public Button AutoButton { get; } = new();
	public Button StopButton { get; } = new();
	private readonly Label progressLabel = new();

	public ControlSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 350;
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 170;
		BackColor = Theme.Panel;

		BuildUi();
	}

	private void BuildUi()
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Play,
			"CONTROL");

		ConfigureButton(StartButton, "START", 20, 65, 115, Theme.Success);
		ConfigureButton(AutoButton, "AUTO", 145, 65, 115, Theme.Accent);
		ConfigureButton(StopButton, "STOP", 270, 65, 115, Theme.Danger);
		progressLabel.SetBounds(20, 120, 365, 24);
		progressLabel.Text = "MANUAL MODE";
		progressLabel.TextAlign = ContentAlignment.MiddleCenter;
		progressLabel.Font = ThemeFonts.SectionHeader;
		progressLabel.ForeColor = Theme.TextMuted;
		progressLabel.BackColor = Theme.Panel;

		Controls.AddRange([
			StartButton,
			AutoButton,
			StopButton,
			progressLabel
		]);
	}

	public void UpdateProgress(string text)
	{
		ControlUpdate.SetText(progressLabel, text);
	}

	private static void ConfigureButton(Button button, string text, int left, int top, int width, Color backColor)
	{
		button.Text = text;
		button.Left = left;
		button.Top = top;
		button.Width = width;
		button.Height = 46;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.BackColor = backColor;
		button.ForeColor = backColor == Theme.Accent ? Color.Black : Color.White;
		button.Font = ThemeFonts.Button;
	}

}
