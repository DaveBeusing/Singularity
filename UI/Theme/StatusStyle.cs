// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;

namespace Singularity.UI;

internal static class StatusStyle
{
	public static Color GetColor(ValidationStatus status)
	{
		return status switch
		{
			ValidationStatus.Pass => Theme.Success,
			ValidationStatus.Warning => Theme.Accent,
			ValidationStatus.Fail => Theme.Danger,
			_ => Theme.TextMuted
		};
	}

	public static string Format(ValidationStatus status) =>
		status.ToString().ToUpperInvariant();
}
