// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Singularity.Core.Workloads;

internal static class NvidiaGpuAdapterResolver
{
	private const int Success = 0;
	private const int LuidSize = 8;

	public static long ResolveAdapterLuid(string selectedIdentifier)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(selectedIdentifier);

		try
		{
			Check(CudaNative.Init(0), "NVIDIA driver initialization failed");
			Check(CudaNative.DeviceGetCount(out int deviceCount), "NVIDIA GPU enumeration failed");

			List<GpuAdapterIdentity> adapters = new(deviceCount);
			for (int ordinal = 0; ordinal < deviceCount; ordinal++)
			{
				if (CudaNative.DeviceGet(out int device, ordinal) != Success)
					continue;

				if (!TryGetUuid(device, out Guid uuid))
					continue;

				byte[] luid = new byte[LuidSize];
				if (CudaNative.DeviceGetLuid(luid, out _, device) != Success)
					continue;

				adapters.Add(new GpuAdapterIdentity(
					Convert.ToHexString(uuid.ToByteArray()),
					BinaryPrimitives.ReadInt64LittleEndian(luid)));
			}

			return GpuAdapterIdentityMapper.ResolveLuid(adapters, selectedIdentifier);
		}
		catch (DllNotFoundException ex)
		{
			throw new InvalidOperationException(
				"NVIDIA driver adapter identity interface is unavailable.",
				ex);
		}
		catch (EntryPointNotFoundException ex)
		{
			throw new InvalidOperationException(
				"The installed NVIDIA driver cannot provide the adapter identity required for explicit GPU selection.",
				ex);
		}
	}

	private static bool TryGetUuid(int device, out Guid uuid)
	{
		try
		{
			return CudaNative.DeviceGetUuidV2(out uuid, device) == Success;
		}
		catch (EntryPointNotFoundException)
		{
			return CudaNative.DeviceGetUuid(out uuid, device) == Success;
		}
	}

	private static void Check(int result, string message)
	{
		if (result != Success)
			throw new InvalidOperationException($"{message} (driver result {result}).");
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
		internal static extern int DeviceGetUuidV2(out Guid uuid, int device);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetUuid")]
		internal static extern int DeviceGetUuid(out Guid uuid, int device);

		[DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetLuid")]
		internal static extern int DeviceGetLuid(
			[Out] byte[] luid,
			out uint deviceNodeMask,
			int device);
	}
}
