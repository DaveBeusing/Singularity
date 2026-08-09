// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.UI.Controls;

internal static class ControlUpdate
{
	public static void SetText(Control control, string text)
	{
		if (!string.Equals(control.Text, text, StringComparison.Ordinal))
			control.Text = text;
	}

	public static void SetForeColor(Control control, Color color)
	{
		if (control.ForeColor != color)
			control.ForeColor = color;
	}

	public static void SetBackColor(Control control, Color color)
	{
		if (control.BackColor != color)
			control.BackColor = color;
	}
}
