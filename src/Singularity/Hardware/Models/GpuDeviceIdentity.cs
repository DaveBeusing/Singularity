// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Singularity.Hardware.Models;

public static class GpuDeviceIdentity
{
	private const string DxgiLuidPrefix = "dxgi:luid:";

	public static string FromAdapterLuid(long adapterLuid) =>
		$"{DxgiLuidPrefix}{unchecked((ulong)adapterLuid):X16}";

	public static bool TryGetAdapterLuid(string? identifier, out long adapterLuid)
	{
		adapterLuid = 0;
		if (string.IsNullOrWhiteSpace(identifier) ||
			!identifier.StartsWith(DxgiLuidPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		ReadOnlySpan<char> value = identifier.AsSpan(DxgiLuidPrefix.Length);
		if (!ulong.TryParse(
			value,
			NumberStyles.AllowHexSpecifier,
			CultureInfo.InvariantCulture,
			out ulong parsed))
		{
			return false;
		}

		adapterLuid = unchecked((long)parsed);
		return true;
	}

	public static string NormalizeVendorIdentifier(string? identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
			return string.Empty;

		return new string(identifier.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
	}

	public static bool IsTransientNvmlIdentifier(string? identifier) =>
		!string.IsNullOrWhiteSpace(identifier) &&
		identifier.StartsWith("nvml:", StringComparison.OrdinalIgnoreCase);
}
