// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Dxc;
using static Vortice.Direct3D12.D3D12;

namespace Singularity.Core.Workloads;

public sealed class GpuStressWorker : IDisposable
{
	private CancellationTokenSource? cancellationTokenSource;
	private Task? workerTask;
	private volatile bool ready;
	private volatile Exception? failure;

	public bool IsRunning => workerTask is { IsCompleted: false };
	public bool IsReady => ready;
	public Exception? Failure => failure;

	public void Start(int targetLoadPercent)
	{
		if (IsRunning)
			return;

		ready = false;
		failure = null;
		cancellationTokenSource = new CancellationTokenSource();
		CancellationToken token = cancellationTokenSource.Token;
		int targetLoad = Math.Clamp(targetLoadPercent, 1, 100);
		workerTask = Task.Run(() => RunAsync(targetLoad, token), token);
	}

	private async Task RunAsync(int targetLoadPercent, CancellationToken cancellationToken)
	{
		try
		{
			using GpuComputeContext context = new();
			ready = true;
			await context.RunAsync(targetLoadPercent, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Expected when the workload is stopped.
		}
		catch (Exception ex)
		{
			failure = ex;
		}
		finally
		{
			ready = false;
		}
	}

	public void Stop()
	{
		if (cancellationTokenSource is null)
			return;

		cancellationTokenSource.Cancel();
		try
		{
			workerTask?.GetAwaiter().GetResult();
		}
		catch (OperationCanceledException)
		{
		}

		workerTask = null;
		cancellationTokenSource.Dispose();
		cancellationTokenSource = null;
		ready = false;
	}

	public void Dispose() => Stop();

	private sealed class GpuComputeContext : IDisposable
	{
		private const ulong BufferSize = 1024 * 1024;
		private const uint ThreadGroupCount = 4096;
		private const int FenceTimeoutMilliseconds = 5000;
		private const string ShaderSource = """
			RWByteAddressBuffer Output : register(u0);

			[numthreads(64, 1, 1)]
			void main(uint3 dispatchId : SV_DispatchThreadID)
			{
				uint state = dispatchId.x + 1;
				[loop]
				for (uint index = 0; index < 512; index++)
				{
					state = state * 1664525u + 1013904223u;
					state ^= state >> 13;
					state *= 2246822519u;
				}

				Output.Store((dispatchId.x & 262143u) * 4u, state);
			}
			""";

		private readonly ID3D12Device device = null!;
		private readonly ID3D12CommandQueue commandQueue = null!;
		private readonly ID3D12CommandAllocator commandAllocator = null!;
		private readonly ID3D12GraphicsCommandList commandList = null!;
		private readonly ID3D12RootSignature rootSignature = null!;
		private readonly ID3D12PipelineState pipelineState = null!;
		private readonly ID3D12Resource outputBuffer = null!;
		private readonly ID3D12Fence fence = null!;
		private readonly AutoResetEvent fenceEvent = new(false);
		private ulong fenceValue;

		public GpuComputeContext()
		{
			try
			{
				if (D3D12CreateDevice(null, FeatureLevel.Level_11_0, out ID3D12Device? createdDevice).Failure ||
					createdDevice is null)
				{
					throw new InvalidOperationException("Direct3D 12 device initialization failed.");
				}

				device = createdDevice;
				commandQueue = device.CreateCommandQueue(CommandListType.Compute);
				commandAllocator = device.CreateCommandAllocator(CommandListType.Compute);

				RootDescriptor1 outputDescriptor = new(0, 0, RootDescriptorFlags.DataVolatile);
				RootParameter1 outputParameter = new(
				RootParameterType.UnorderedAccessView,
				outputDescriptor,
				ShaderVisibility.All);
				rootSignature = device.CreateRootSignature(
				new RootSignatureDescription1(
					RootSignatureFlags.None,
					[outputParameter]));

				ReadOnlyMemory<byte> shaderBytecode = CompileShader();
				pipelineState = device.CreateComputePipelineState<ID3D12PipelineState>(
				new ComputePipelineStateDescription
				{
					RootSignature = rootSignature,
					ComputeShader = shaderBytecode
				});

				outputBuffer = device.CreateCommittedResource(
				HeapType.Default,
				ResourceDescription.Buffer(BufferSize, ResourceFlags.AllowUnorderedAccess),
				ResourceStates.UnorderedAccess);

				commandList = device.CreateCommandList<ID3D12GraphicsCommandList>(
				CommandListType.Compute,
				commandAllocator,
				pipelineState);
				commandList.SetComputeRootSignature(rootSignature);
				commandList.SetComputeRootUnorderedAccessView(0, outputBuffer.GPUVirtualAddress);
				commandList.Dispatch(ThreadGroupCount, 1, 1);
				commandList.Close();

				fence = device.CreateFence(0);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public async Task RunAsync(int targetLoadPercent, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				Stopwatch activeTime = Stopwatch.StartNew();
				ExecuteAndWait();
				activeTime.Stop();

				if (targetLoadPercent >= 100)
					continue;

				double idleMilliseconds =
					activeTime.Elapsed.TotalMilliseconds * (100 - targetLoadPercent) / targetLoadPercent;
				if (idleMilliseconds >= 1)
				{
					await Task.Delay(
						TimeSpan.FromMilliseconds(idleMilliseconds),
						cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private void ExecuteAndWait()
		{
			commandQueue.ExecuteCommandList(commandList);
			ulong nextFenceValue = ++fenceValue;
			commandQueue.Signal(fence, nextFenceValue).CheckError();

			if (fence.CompletedValue >= nextFenceValue)
				return;

			fence.SetEventOnCompletion(nextFenceValue, fenceEvent).CheckError();
			if (!fenceEvent.WaitOne(FenceTimeoutMilliseconds))
				throw new TimeoutException("Direct3D 12 GPU workload timed out.");
		}

		private static ReadOnlyMemory<byte> CompileShader()
		{
			DxcCompilerOptions options = new()
			{
				ShaderModel = DxcShaderModel.Model6_0
			};
			using IDxcResult result = DxcCompiler.Compile(
				DxcShaderStage.Compute,
				ShaderSource,
				"main",
				options,
				fileName: "GpuStress.hlsl");

			if (result.GetStatus().Failure)
				throw new InvalidOperationException($"GPU shader compilation failed: {result.GetErrors()}");

			return result.GetObjectBytecodeMemory();
		}

		public void Dispose()
		{
			fence?.Dispose();
			fenceEvent.Dispose();
			commandList?.Dispose();
			commandAllocator?.Dispose();
			outputBuffer?.Dispose();
			pipelineState?.Dispose();
			rootSignature?.Dispose();
			commandQueue?.Dispose();
			device?.Dispose();
		}
	}
}
