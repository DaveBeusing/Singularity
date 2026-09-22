// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

public class CommandButton : Button
{
	public CommandButton()
	{
		FlatStyle = FlatStyle.Flat;
		FlatAppearance.BorderSize = 0;
		FlatAppearance.MouseOverBackColor = Theme.Hover;
		FlatAppearance.MouseDownBackColor = Theme.Pressed;
		BackColor = Theme.PanelLight;
		ForeColor = Theme.TextMain;
		Font = ThemeFonts.Button;
		UseVisualStyleBackColor = false;
		TabStop = true;
	}

	protected override void OnPaint(PaintEventArgs pevent)
	{
		base.OnPaint(pevent);

		if (!Focused || !ShowFocusCues || Width < 3 || Height < 3)
			return;

		using Pen pen = new(Theme.Focus);
		pevent.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
	}
}
