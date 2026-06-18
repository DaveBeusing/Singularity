# Singularity

Modern Windows hardware inventory, monitoring and stress testing toolkit built with .NET 8 and WinForms.

![Platform](https://img.shields.io/badge/.NET-8.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Overview

Singularity is a lightweight desktop application for:

- Hardware inventory
- System monitoring
- Hardware validation
- Stress testing
- System diagnostics

The project focuses on native Windows APIs, WMI, NVML and custom WinForms controls while avoiding heavy external dependencies.

---

## Features

### Hardware Inventory

- Mainboard information
  - Manufacturer
  - Product name
  - BIOS version
  - BIOS release date

- CPU information
  - Model
  - Core count
  - Thread count
  - Cache sizes
  - Socket
  - Virtualization support

- Memory inventory
  - Installed modules
  - Capacity
  - Speed
  - DDR generation
  - ECC detection
  - DIMM type
  - JEDEC manufacturer decoding

- Storage inventory
  - HDD
  - SATA SSD
  - NVMe SSD
  - Manufacturer detection
  - Bus type detection
  - Firmware revision

- GPU inventory
  - NVIDIA NVML integration
  - VRAM capacity
  - Temperature
  - PCIe generation
  - PCIe link width

- Operating system information
  - Windows version
  - Build number
  - Architecture
  - Install date
  - Boot time

---

### Monitoring

Real-time monitoring of:

- Process CPU utilization
- Process memory usage
- Physical memory consumption
- System memory utilization

---

### Stress Testing

Available workloads:

- CPU workload
- Memory workload
- GPU workload

Workloads can be combined and controlled through the central WorkloadController.

---

## Architecture

```text
UI
 │
 ├─ Sections
 ├─ Panels
 └─ Custom Controls

Hardware
 │
 ├─ Providers
 │   ├─ CpuProvider
 │   ├─ MemoryProvider
 │   ├─ StorageProvider
 │   ├─ MainboardProvider
 │   ├─ OsProvider
 │   └─ NvmlGpuProvider
 │
 ├─ Models
 ├─ Decoders
 └─ Native
     └─ NVML

Monitoring
 │
 ├─ SystemMonitor
 └─ SystemSnapshot

Core
 │
 ├─ WorkloadController
 └─ WorkloadOptions

Workloads
 │
 ├─ CpuWorkload
 ├─ MemoryWorkload
 └─ GpuWorkload
```

---

## Technology Stack

- C#
- .NET 8
- Windows Forms
- WMI
- NVIDIA NVML
- Native Win32 APIs

---

## Building

### Requirements

- Windows 10 / 11
- .NET 8 SDK
- NVIDIA Driver (optional for GPU inventory)

### Restore

```powershell
dotnet restore
```

### Build

```powershell
dotnet build -c Release
```

### Run

```powershell
dotnet run
```

Or use the provided build script:

```powershell
.\build.ps1 -Run
```

---

## Repository Utilities

### Repository Scan

Generate a complete source scan:

```powershell
.\scan.ps1
```

Generated files:

- repo_tree.txt
- repo_scan.txt

---

## Roadmap

### Inventory

- [x] Mainboard
- [x] CPU
- [x] Memory
- [x] Storage
- [x] GPU
- [ ] Network adapters
- [ ] PCIe topology

### Monitoring

- [x] Memory
- [x] Process CPU
- [ ] Hardware sensors
- [ ] Historical charts
- [ ] Export functionality

### Stress Testing

- [x] CPU
- [x] Memory
- [x] GPU
- [ ] Presets
- [ ] Result reports
- [ ] Validation profiles

---

## License

Licensed under the MIT License.

Copyright (c) 2026 David Beusing