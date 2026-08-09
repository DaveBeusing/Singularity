// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Workloads;

namespace Singularity.Core.Qualification;

public sealed record QualificationStep(
	string Name,
	TimeSpan Duration,
	WorkloadOptions Workload);
