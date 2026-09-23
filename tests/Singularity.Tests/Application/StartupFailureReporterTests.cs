// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;

namespace Singularity.Tests.Application;

public sealed class StartupFailureReporterTests
{
	[Fact]
	public void BuildDiagnosticReport_IncludesFailureContextAndException()
	{
		InvalidOperationException exception = new("Simulated startup failure.");

		string report = StartupFailureReporter.BuildDiagnosticReport(
			"Application startup failed",
			exception);

		Assert.Contains("Context: Application startup failed", report, StringComparison.Ordinal);
		Assert.Contains("InvalidOperationException", report, StringComparison.Ordinal);
		Assert.Contains("Simulated startup failure.", report, StringComparison.Ordinal);
		Assert.Contains("Application version:", report, StringComparison.Ordinal);
		Assert.Contains("Base directory:", report, StringComparison.Ordinal);
		Assert.Contains("Current directory:", report, StringComparison.Ordinal);
	}
}
