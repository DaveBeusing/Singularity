// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Management;
using Singularity.Hardware.Models;

namespace Singularity.Hardware.Providers;

public sealed class MainboardProvider
{
	public MainboardInventory Read()
	{
		return new MainboardInventory
		{
			Manufacturer = GetWmiValue("Win32_BaseBoard", "Manufacturer"),
			Product = GetWmiValue("Win32_BaseBoard", "Product"),
			BiosVersion = GetWmiValue("Win32_BIOS", "SMBIOSBIOSVersion"),
			BiosDate = GetWmiValue("Win32_BIOS", "ReleaseDate")
		};
	}

	private static string GetWmiValue(string className, string property)
	{
		try
		{
			using ManagementObjectSearcher searcher = new($"SELECT {property} FROM {className}");
			foreach (ManagementObject obj in searcher.Get())
			{
				return obj[property]?.ToString() ?? "Unknown";
			}
		}
		catch
		{
		}
		return "Unknown";
	}

}