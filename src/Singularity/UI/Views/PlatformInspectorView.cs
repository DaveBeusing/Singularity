// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class PlatformInspectorView : Panel
{
	private readonly Label headingLabel = new();
	private readonly Label emptyLabel = new();
	private readonly Panel rowsHost = new();

	public PlatformInspectorView()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Inspector;
		Padding = new Padding(ThemeMetrics.Spacing);

		headingLabel.Dock = DockStyle.Top;
		headingLabel.Height = 42;
		headingLabel.Font = ThemeFonts.Header;
		headingLabel.ForeColor = Theme.TextMain;
		headingLabel.BackColor = Theme.Inspector;
		headingLabel.TextAlign = ContentAlignment.MiddleLeft;
		headingLabel.Text = "DEVICE DETAILS";

		emptyLabel.Dock = DockStyle.Fill;
		emptyLabel.Font = ThemeFonts.Subtitle;
		emptyLabel.ForeColor = Theme.TextMuted;
		emptyLabel.BackColor = Theme.Inspector;
		emptyLabel.TextAlign = ContentAlignment.TopLeft;
		emptyLabel.Text = "Select a memory module, GPU, or storage device to inspect its metadata.";

		rowsHost.Dock = DockStyle.Fill;
		rowsHost.AutoScroll = true;
		rowsHost.BackColor = Theme.Inspector;

		Controls.Add(rowsHost);
		Controls.Add(headingLabel);
		ShowSelection(null);
	}

	public void ShowSelection(PlatformDeviceSelection? selection)
	{
		foreach (Control control in rowsHost.Controls.Cast<Control>().ToArray())
		{
			rowsHost.Controls.Remove(control);
			if (!ReferenceEquals(control, emptyLabel))
				control.Dispose();
		}

		if (selection is null)
		{
			headingLabel.Text = "DEVICE DETAILS";
			rowsHost.Controls.Add(emptyLabel);
			return;
		}

		headingLabel.Text = $"{selection.Kind.ToUpperInvariant()} • {selection.DisplayName}";

		for (int index = selection.Properties.Count - 1; index >= 0; index--)
		{
			PlatformProperty property = selection.Properties[index];
			rowsHost.Controls.Add(new PropertyValueRow(property.Name, property.Value));
		}
	}
}
