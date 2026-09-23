// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

# GPU Inventory

Singularity treats Windows graphics discovery and vendor-specific telemetry as separate concerns.

## Discovery ownership

`WindowsGpuProvider` owns baseline GPU inventory. It enumerates non-software DXGI adapters, records the DXGI adapter LUID as the baseline identity, captures vendor/device metadata and dedicated video memory, and probes whether the adapter can create a Direct3D 12 device at feature level 11_0.

Baseline discovery is independent of NVML. AMD, Intel, NVIDIA, and other usable Windows graphics adapters can therefore appear in Platform inventory even when no NVIDIA runtime is installed.

Software adapters are excluded from the normal hardware inventory. Adapters that are discoverable but cannot create the required Direct3D 12 device remain visible in Platform inventory but are not offered as qualification workload targets.

## Canonical identity

The baseline canonical identifier uses:

```text
dxgi:luid:<16 hexadecimal digits>
```

The adapter LUID is the Windows graphics identity used to correlate inventory with Direct3D workload placement.

For NVIDIA adapters, an immutable NVIDIA GPU UUID may replace the LUID-form identifier after successful enrichment because existing qualification history and selection contracts already use the NVIDIA UUID. The merged device retains its Windows adapter LUID so Direct3D placement remains deterministic.

Transient identifiers such as `nvml:<index>` and `nvml:unavailable` are never treated as stable qualification identities.

## NVIDIA enrichment

`NvmlGpuProvider` remains an NVIDIA-specific enrichment provider for name, memory, temperature, and PCIe details.

Where available, `NvidiaGpuAdapterIdentityProvider` uses the NVIDIA CUDA driver API only as an optional identity bridge between the stable NVIDIA UUID and the Windows adapter LUID. Failure or absence of that bridge does not prevent DXGI baseline inventory from discovering the adapter.

`GpuInventoryMerger` merges a matching NVML device into the Windows baseline without adding a duplicate adapter. Matching prefers the Windows adapter LUID and then stable identifiers. Unmatched transient NVML fallback entries are not appended to the baseline inventory.

If Windows baseline discovery is unavailable, stable NVML devices may be retained as a degraded NVIDIA-only inventory fallback. Transient NVML fallback entries are excluded.

## Unavailable data

Vendor-neutral discovery does not fabricate vendor-specific telemetry.

Fields such as GPU temperature, PCIe generation, PCIe width, and other vendor telemetry remain `Unavailable` when the active provider cannot supply them. UI and report formatting must preserve that distinction instead of rendering misleading numeric zero values.

## Platform and reporting behavior

Platform presentation uses the same inventory model for all vendors and includes vendor, vendor/device IDs, Direct3D 12 capability, adapter LUID, memory, and available enrichment.

Qualification selection accepts only stable identities for adapters that are Direct3D 12 capable.

JSON and HTML reporting consume the merged hardware inventory. Non-NVIDIA adapters must therefore remain visible in hardware summaries even when NVIDIA telemetry is absent.

## Manual validation matrix

| Scenario | Expected result |
| --- | --- |
| NVIDIA with NVML and CUDA driver API available | One NVIDIA adapter per physical device; stable UUID identity; DXGI LUID retained; NVML fields enriched; Direct3D workload resolves to the selected adapter. |
| NVIDIA with NVML unavailable | NVIDIA adapter remains visible through DXGI; LUID identity is stable for the running Windows installation; vendor-specific telemetry remains unavailable; no synthetic NVML device is shown. |
| NVIDIA with CUDA identity bridge unavailable | DXGI inventory remains available; missing UUID/LUID correlation must not create duplicate devices; unsupported enrichment remains explicit. |
| AMD discrete GPU | Adapter is visible through DXGI with AMD vendor metadata, dedicated memory, LUID identity, and Direct3D capability; NVIDIA-only telemetry is unavailable. |
| Intel integrated/discrete GPU | Adapter is visible through DXGI with Intel vendor metadata, memory reported by DXGI, LUID identity, and Direct3D capability; NVIDIA-only telemetry is unavailable. |
| Hybrid graphics | Every physical/usable non-software DXGI adapter is represented once; ordering is deterministic from DXGI enumeration; qualification options include only Direct3D 12-capable adapters. |
| Multi-adapter NVIDIA/AMD/Intel | No duplicate devices after enrichment; each qualification selection preserves its stable identity and maps to the intended DXGI adapter. |
| Microsoft Basic Render/software adapter | Excluded from normal physical GPU inventory. |
| Adapter without required Direct3D 12 support | Visible in Platform inventory when discovered, clearly marked unavailable for Direct3D 12, and excluded from qualification GPU choices. |

## Automated coverage

The deterministic test suite covers baseline-only adapters, NVML enrichment merge, multiple adapters, duplicate prevention, unavailable fields, stable identity/order behavior, qualification option filtering, Platform mapping, and report hardware summary mapping.

Ordinary CI must not require a physical GPU, NVML, CUDA, administrator elevation, or an interactive desktop.
