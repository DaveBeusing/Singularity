// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Hardware.Models;
using Singularity.Hardware.Utils;

namespace Singularity.Hardware.Providers;

public sealed class CpuProvider
{
	public CpuInventory Read()
	{
		try
		{
			foreach (var obj in WmiQuery.Execute(
				@"SELECT
					Name,
					NumberOfCores,
					NumberOfLogicalProcessors,
					MaxClockSpeed,
					L2CacheSize,
					L3CacheSize,
					SocketDesignation,
					VirtualizationFirmwareEnabled
				FROM Win32_Processor"))
			{
				return new CpuInventory
				{
					Name = WmiConverter.ToString(obj["Name"]),
					CoreCount = (int)WmiConverter.ToUInt32(obj["NumberOfCores"]),
					ThreadCount = (int)WmiConverter.ToUInt32(obj["NumberOfLogicalProcessors"]),
					MaxClockMhz = (int)WmiConverter.ToUInt32(obj["MaxClockSpeed"]),
					L2CacheKb = (int)WmiConverter.ToUInt32(obj["L2CacheSize"]),
					L3CacheKb = (int)WmiConverter.ToUInt32(obj["L3CacheSize"]),
					Socket = WmiConverter.ToString(obj["SocketDesignation"]),
					VirtualizationEnabled =  WmiConverter.ToBoolean(obj["VirtualizationFirmwareEnabled"])
				};
			}
		}
		catch
		{
		}
		return new CpuInventory();
	}
}