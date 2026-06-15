// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Decoders;

public static class MemoryTypeDecoder
{
	public static string Decode(ushort memoryType, ushort smbiosMemoryType)
	{
		return smbiosMemoryType switch
		{
			24 => "DDR3",
			26 => "DDR4",
			34 => "DDR5",
			_ => memoryType switch
			{
				24 => "DDR3",
				26 => "DDR4",
				34 => "DDR5",
				_ => "Unknown"
			}
		};
	}
}