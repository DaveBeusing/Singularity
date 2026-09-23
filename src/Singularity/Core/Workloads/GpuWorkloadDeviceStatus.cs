// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Core.Workloads;

public sealed record GpuWorkloadDeviceStatus(
	string Identifier,
	WorkloadState State,
	string Message);
