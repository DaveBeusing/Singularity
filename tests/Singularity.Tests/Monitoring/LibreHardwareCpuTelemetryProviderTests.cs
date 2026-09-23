// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using LibreHardwareMonitor.Hardware;
using Singularity.Monitoring.Providers;

namespace Singularity.Tests.Monitoring;

public sealed class LibreHardwareCpuTelemetryProviderTests
{
	[Fact]
	public void InitializationFailure_ReportsTelemetryUnavailable()
	{
		using LibreHardwareCpuTelemetryProvider provider = new(
			() => throw new InvalidOperationException("Simulated hardware initialization failure."));

		var snapshot = provider.Read();

		Assert.False(snapshot.IsAvailable);
		Assert.Equal("CPU temp initialization failed", snapshot.Status);
	}
}
