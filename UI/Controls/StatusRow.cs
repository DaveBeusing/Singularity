// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Layout;

namespace Singularity.UI.Controls;

public sealed class StatusRow : Panel
{
	private readonly Label statusLabel;

	public StatusRow(string title)
	{
		Width = LayoutConstants.SectionContentWidth;
		Height = LayoutConstants.StatusRowHeight;
		BackColor = Theme.PanelLight;

		Label titleLabel = new()
		{
			Text = title,
			Left = 14,
			Top = 0,
			Width = 180,
			Height = Height,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Theme.TextMain,
			Font = ThemeFonts.CardTitle,
			BackColor = Theme.PanelLight
		};

		statusLabel = new Label
		{
			Text = "UNKNOWN",
			Left = Width - 120,
			Top = 0,
			Width = 100,
			Height = Height,
			TextAlign = ContentAlignment.MiddleRight,
			ForeColor = Theme.TextMuted,
			Font = ThemeFonts.CardTitle,
			BackColor = Theme.PanelLight
		};

		Controls.AddRange([titleLabel, statusLabel]);
	}

	public void SetStatus(ValidationStatus status)
	{
		ControlUpdate.SetText(statusLabel, StatusStyle.Format(status));
		ControlUpdate.SetForeColor(statusLabel, StatusStyle.GetColor(status));
	}

	public void Reset() => SetStatus(ValidationStatus.Unknown);
}
