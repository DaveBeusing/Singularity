// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Hardware.Utils;

internal static class WmiConverter
{
	public static string ToString(object? value, string fallback = "Unknown")
	{
		return value?.ToString()?.Trim() ?? fallback;
	}

	public static ushort ToUInt16(object? value)
	{
		if (value == null)
			return 0;
		try
		{
			return Convert.ToUInt16(value);
		}
		catch
		{
			return 0;
		}
	}

	public static uint ToUInt32(object? value)
	{
		if (value == null)
			return 0;
		try
		{
			return Convert.ToUInt32(value);
		}
		catch
		{
			return 0;
		}
	}

	public static ulong ToUInt64(object? value)
	{
		if (value == null)
			return 0;
		try
		{
			return Convert.ToUInt64(value);
		}
		catch
		{
			return 0;
		}
	}

	public static bool ToBoolean(object? value)
	{
		if (value == null)
			return false;
		try
		{
			return Convert.ToBoolean(value);
		}
		catch
		{
			return false;
		}
	}

}