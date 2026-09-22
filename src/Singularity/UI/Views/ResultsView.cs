// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Layout;
using Singularity.UI.Sections;

namespace Singularity.UI.Views;

public sealed class ResultsView : Panel
{
	private readonly SessionSection sessionSection;
	private readonly ValidationSection validationSection;

	public ResultsView()
	{
		Left = 0;
		Top = 0;
		Width = LayoutConstants.MainWidth;
		BackColor = Theme.Background;

		sessionSection = new SessionSection
		{
			Left = 0,
			Top = 0
		};

		validationSection = new ValidationSection
		{
			Left = LayoutConstants.SidePanelLeft,
			Top = 0
		};

		Controls.AddRange([
			sessionSection,
			validationSection
		]);

		Height = Math.Max(sessionSection.Bottom, validationSection.Bottom);
	}

	public void UpdateSession(QualificationSession session)
	{
		sessionSection.UpdateSession(session);
	}

	public void UpdateValidation(ValidationResult result)
	{
		validationSection.UpdateValidation(result);
	}

	public void ResetValidation()
	{
		validationSection.Reset();
	}
}
