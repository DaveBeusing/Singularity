// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Singularity.Hardware.Native.Nvml;

[StructLayout(LayoutKind.Sequential)]
internal struct NvmlUtilization
{
	public uint Gpu;
	public uint Memory;

}