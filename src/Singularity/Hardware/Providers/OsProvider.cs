// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Management;
using System.Runtime.InteropServices;
using Singularity.Hardware.Models;
using Singularity.Hardware.Utils;

namespace Singularity.Hardware.Providers;

public sealed class OsProvider
{
	public OsInventory Read()
	{
		try
		{
			foreach (var obj in WmiQuery.Execute(
				@"SELECT
					Caption,
					Version,
					OSArchitecture,
					BuildNumber,
					InstallDate,
					LastBootUpTime
					FROM Win32_OperatingSystem"))
			{
				return new OsInventory
				{
					Name = WmiConverter.ToString(obj["Caption"]),
					Version = WmiConverter.ToString(obj["Version"]),
					Architecture = WmiConverter.ToString(obj["OSArchitecture"]),
					Build = WmiConverter.ToString(obj["BuildNumber"]),
					InstallDate = ParseBootTime(WmiConverter.ToString(obj["InstallDate"], string.Empty)),
					ComputerName = Environment.MachineName,
					UserName = Environment.UserName,
					BootTime = ParseBootTime(WmiConverter.ToString(obj["LastBootUpTime"], string.Empty))
				};
			}
		}
		catch
		{
		}
		return new OsInventory();
	}

	private static DateTime ParseBootTime(string value)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(value))
				return DateTime.MinValue;
			return ManagementDateTimeConverter.ToDateTime(value);
		}
		catch
		{
			return DateTime.MinValue;
		}
	}

}