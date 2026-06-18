// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Controls;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI.Sections;

public sealed class ControlSection : Panel
{
	public Button StartButton { get; } = new();
	public Button StopButton { get; } = new();

	public ControlSection()
	{
		Left = LayoutConstants.SidePanelLeft;
		Top = 350;
		Width = LayoutConstants.MetricsPanelWidth;
		Height = 150;
		BackColor = Theme.Panel;

		BuildUi();
	}

	private void BuildUi()
	{
		UiFactory.AddSectionHeader(
			this,
			SingularityIconType.Play,
			"CONTROL");

		ConfigureButton(StartButton, "START", 20, 70, Theme.Success);
		ConfigureButton(StopButton, "STOP", 210, 70, Theme.Danger);

		Controls.AddRange([
			StartButton,
			StopButton
		]);
	}

	private static void ConfigureButton(Button button, string text, int left, int top, Color backColor)
	{
		button.Text = text;
		button.Left = left;
		button.Top = top;
		button.Width = 170;
		button.Height = 46;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.BackColor = backColor;
		button.ForeColor = Color.White;
		button.Font = ThemeFonts.Button;
	}

}