// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Decoders;

public static class JedecManufacturerDecoder
{
	private static readonly Dictionary<string, string>
		Manufacturers = new()
		{
			["80CE"] = "Samsung",
			["80AD"] = "SK Hynix",
			["802C"] = "Micron",
			["04CD"] = "G.Skill",
			["029E"] = "Corsair",
			["04CB"] = "ADATA",
			["859B"] = "Crucial",
			["7F7F7F0B"] = "Kingston"
		};

	public static string Decode(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
			return "Unknown";
		return Manufacturers.TryGetValue(code.ToUpperInvariant(), out string? manufacturer) ? manufacturer : code;
	}

}