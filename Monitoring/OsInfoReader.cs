// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Management;

namespace Singularity.Monitoring;

public sealed class OsInfoReader
{
	public OsInfo Read()
	{
		try
		{
			using ManagementObjectSearcher searcher = new("SELECT Caption, Version, OSArchitecture, BuildNumber, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
			foreach (ManagementObject obj in searcher.Get())
			{
				return new OsInfo
				{
					Name = Clean(obj["Caption"]),
					Version = Clean(obj["Version"]),
					Architecture = Clean(obj["OSArchitecture"]),
					BuildNumber = Clean(obj["BuildNumber"]),
					InstallDate = FormatWmiDate(Clean(obj["InstallDate"])),
					LastBootTime = FormatWmiDate(Clean(obj["LastBootUpTime"])),
					User = Environment.UserName
				};
			}
		}
		catch
		{
		}
		return new OsInfo();
	}

	private static string Clean(object? value)
	{
		if (value == null)
			return "Unknown";
		string text = value.ToString()?.Trim() ?? "Unknown";
		return string.IsNullOrWhiteSpace(text) ? "Unknown" : text;
	}

	private static string FormatWmiDate(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw) || raw.Length < 14)
			return "Unknown";

		try
		{
			int year = int.Parse(raw[..4]);
			int month = int.Parse(raw.Substring(4, 2));
			int day = int.Parse(raw.Substring(6, 2));
			int hour = int.Parse(raw.Substring(8, 2));
			int minute = int.Parse(raw.Substring(10, 2));
			int second = int.Parse(raw.Substring(12, 2));
			DateTime date = new(year, month, day, hour, minute, second);
			return date.ToString("yyyy-MM-dd HH:mm:ss");
		}
		catch
		{
			return "Unknown";
		}
	}

}