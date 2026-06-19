// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.UI.Controls;

public sealed class ValidationItemControl : Panel
{
	private readonly Label titleLabel;
	private readonly Label statusLabel;

	public ValidationItemControl(
		string title,
		int width,
		int height)
	{
		Width = width;
		Height = height;

		BackColor = Theme.PanelLight;

		titleLabel = new Label
		{
			Text = title,
			Left = 14,
			Top = 0,
			Width = 180,
			Height = height,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Theme.TextMain,
			Font = ThemeFonts.CardTitle,
			BackColor = Theme.PanelLight
		};

		statusLabel = new Label
		{
			Text = "UNKNOWN",
			Left = width - 120,
			Top = 0,
			Width = 100,
			Height = height,
			TextAlign = ContentAlignment.MiddleRight,
			ForeColor = Theme.TextMuted,
			Font = ThemeFonts.CardTitle,
			BackColor = Theme.PanelLight
		};

		Controls.Add(titleLabel);
		Controls.Add(statusLabel);
	}

	public void UpdateStatus(
		ValidationStatus status,
		string _)
	{
		switch (status)
		{
			case ValidationStatus.Pass:
				statusLabel.Text = "PASS";
				statusLabel.ForeColor = Theme.Success;
				break;

			case ValidationStatus.Warning:
				statusLabel.Text = "WARNING";
				statusLabel.ForeColor = Theme.Accent;
				break;

			case ValidationStatus.Fail:
				statusLabel.Text = "FAIL";
				statusLabel.ForeColor = Theme.Danger;
				break;

			default:
				statusLabel.Text = "UNKNOWN";
				statusLabel.ForeColor = Theme.TextMuted;
				break;
		}
	}

	public void Reset()
	{
		UpdateStatus(
			ValidationStatus.Unknown,
			string.Empty);
	}

}