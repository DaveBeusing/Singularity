// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Singularity.Hardware.Providers;

public sealed record NvidiaGpuAdapterIdentity(
	string Identifier,
	long AdapterLuid);

public static class NvidiaGpuAdapterIdentityProvider
{
	private const int Success = 0;
	private const int UuidSize = 16;
	private const int LuidSize = 8;

	public static IReadOnlyList<NvidiaGpuAdapterIdentity> ReadAll()
	{
		try
		{
			if (CudaNative.Init(0) != Success ||
				CudaNative.DeviceGetCount(out int deviceCount) != Success ||
				deviceCount <= 0)
			{
				return Array.Empty<NvidiaGpuAdapterIdentity>();
			}

			List<NvidiaGpuAdapterIdentity> identities = new(deviceCount);
			for (int ordinal = 0; ordinal < deviceCount; ordinal++)
			{
				if (CudaNative.DeviceGet(out int device, ordinal) != Success ||
					!TryGetUuid(device, out byte[] uuid))
				{
					continue;
				}

				byte[] luid = new byte[LuidSize];
				if (CudaNative.DeviceGetLuid(luid, out _, device) != Success)
					continue;

				identities.Add(new NvidiaGpuAdapterIdentity(
					FormatUuid(uuid),
					BinaryPrimitives.ReadInt64LittleEndian(luid)));
			}

			return identities.AsReadOnly();
		}
		catch (DllNotFoundException)
		{
			return Array.Empty<NvidiaGpuAdapterIdentity>();
		}
		catch (EntryPointNotFoundException)
		{
			return Array.Empty<NvidiaGpuAdapterIdentity>();
		}
		catch
		{
			return Array.Empty<NvidiaGpuAdapterIdentity>();
		}
	}

	private static bool TryGetUuid(int device, out byte[] uuid)
	{
		uuid = new byte[UuidSize];
		try
		{
			return CudaNative.DeviceGetUuidV2(uuid, device) == Success;
		}
		catch (EntryPointNotFoundException)
		{
			return CudaNative.DeviceGetUuid(uuid, device) == Success;
		}
	}

	private static string FormatUuid(byte[] uuid)
	{
		string value = Convert.ToHexString(uuid);
		return $"GPU-{value[..8]}-{value[8..12]}-{value[12..16]}-{value[16..20]}-{value[20..32]}";
	}

	private static class CudaNative
	{
		[DllImport("nvcuda.dll", EntryPoint = "cuInit")]
		internal static extern int Init(uint flags);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetCount")]
		internal static extern int DeviceGetCount(out int count);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGet")]
		internal static extern int DeviceGet(out int device, int ordinal);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetUuid_v2")]
		internal static extern int DeviceGetUuidV2([Out] byte[] uuid, int device);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetUuid")]
		internal static extern int DeviceGetUuid([Out] byte[] uuid, int device);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetLuid")]
		internal static extern int DeviceGetLuid(
			[Out] byte[] luid,
			out uint deviceNodeMask,
			int device);
	}
}
