// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Monitoring;

internal sealed class TelemetryCache
{
	private readonly object syncRoot = new();
	private readonly SystemSnapshot snapshot = new();

	public void Update(Action<SystemSnapshot> update)
	{
		lock (syncRoot)
		{
			update(snapshot);
		}
	}

	public SystemSnapshot GetSnapshot()
	{
		lock (syncRoot)
		{
			return snapshot.Copy();
		}
	}
}
