// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Decoders;

public static class DimmTypeDecoder
{
	public static string Decode(ushort formFactor)
	{
		return formFactor switch
		{
			8 => "DIMM",
			12 => "SODIMM",
			_ => "Unknown"
		};
	}

}