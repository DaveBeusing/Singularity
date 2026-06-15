// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Decoders;

public static class EccDecoder
{
	public static string Decode(ushort totalWidth, ushort dataWidth)
	{
		if (totalWidth == 0 || dataWidth == 0)
		{
			return "Unknown";
		}
		//ECC hat 8bits mehr ;)
		return totalWidth > dataWidth ? "ECC" : "Non-ECC";
	}
}