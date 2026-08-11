// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Singularity.Application;

public static class ApplicationMetadata
{
	public static string Version { get; } = ReadVersion();

	private static string ReadVersion()
	{
		Assembly assembly = typeof(ApplicationMetadata).Assembly;
		string informationalVersion =
			assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString(3)
			?? "unknown";
		int metadataSeparator = informationalVersion.IndexOf('+');
		if (metadataSeparator >= 0)
			informationalVersion = informationalVersion[..metadataSeparator];
		return informationalVersion.StartsWith('v')
			? informationalVersion
			: $"v{informationalVersion}";
	}
}

