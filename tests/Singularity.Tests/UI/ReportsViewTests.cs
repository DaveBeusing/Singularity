// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.UI.Views;

namespace Singularity.Tests.UI;

public sealed class ReportsViewTests
{
	[Fact]
	public void Constructor_DoesNotRequireParentLayout()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ReportsView view = new();
			}
			catch (Exception ex)
			{
				failure = ex;
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();

		Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "ReportsView construction timed out.");
		Assert.Null(failure);
	}
}
