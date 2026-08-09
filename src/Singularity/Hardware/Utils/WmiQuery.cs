// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Management;

namespace Singularity.Hardware.Utils;

internal static class WmiQuery
{
	public static IEnumerable<ManagementObject> Execute(string query)
	{
		using ManagementObjectSearcher searcher = new(query);
		foreach (ManagementObject obj in searcher.Get())
		{
			yield return obj;
		}
	}

	public static IEnumerable<ManagementObject> Execute(string scopePath, string query)
	{
		ManagementScope scope = new(scopePath);
		scope.Connect();
		using ManagementObjectSearcher searcher = new(scope, new ObjectQuery(query));
		foreach (ManagementObject obj in searcher.Get())
		{
			yield return obj;
		}
	}

	public static string GetSingleValue(string className, string propertyName)
	{
		try
		{
			string query = $"SELECT {propertyName} FROM {className}";
			foreach (ManagementObject obj in Execute(query))
			{
				return WmiConverter.ToString(obj[propertyName]);
			}
		}
		catch
		{
		}
		return "Unknown";
	}

}