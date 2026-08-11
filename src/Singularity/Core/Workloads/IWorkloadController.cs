// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public interface IWorkloadController
{
	bool IsRunning { get; }
	WorkloadStatus Status { get; }

	void Start(WorkloadOptions options);
	void Stop();
	void ResetFailure();
}

