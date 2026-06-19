# Singularity

> Platform Qualification Suite for Windows

Singularity is a lightweight Windows-based platform qualification tool written in C# and WinForms.

The goal of the project is to provide a self-contained platform validation environment capable of inventory collection, telemetry monitoring, workload execution and qualification reporting without relying on large external software suites.


![Platform](https://img.shields.io/badge/.NET-10.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/License-MIT-green)
---

## Features

### Platform Inventory

Collects detailed hardware and operating system information.

#### Operating System

- Name
- Version
- Architecture

#### Mainboard

- Manufacturer
- Model
- BIOS Version

#### CPU

- Manufacturer
- Model
- Core Count
- Thread Count

#### GPU

- Manufacturer
- Model
- Dedicated Memory

#### Memory

- Manufacturer
- Model
- Capacity
- Speed

#### Storage

- Manufacturer
- Model
- Capacity
- Bus Type
  - NVMe
  - SATA
  - USB

---

## Telemetry

Real-time monitoring.

### CPU

- Utilization

### GPU

- Utilization
- Temperature
- Memory Usage
- Power Consumption

### System

- Memory Usage

---

## Workloads

### CPU Stress

Configurable CPU thread workload.

### Memory Stress

Configurable memory allocation workload using native Windows memory allocation.

Current implementation:

- VirtualAlloc
- VirtualFree

Immediate memory release after workload stop.

---

## Validation

Built-in qualification validation.

### CPU Validation

Validates achieved CPU utilization.

### Memory Validation

Validates requested memory allocation.

### Overall Validation

Aggregated qualification result.

Possible states:

- PASS
- WARNING
- FAIL

---

## Qualification Session

Tracks execution details.

### Session Data

- Start Time
- Duration
- Result

### Session States

- Idle
- Running
- Completed
- Failed

---

## Qualification History

Stores recent qualification runs.

### History Data

- Result
- Duration
- Start Time

History size:

```text
10 Sessions
```

---

## Architecture

```text
Singularity
│
├─ Hardware
│  ├─ Providers
│  ├─ Models
│  └─ Inventory
│
├─ Monitoring
│  ├─ Telemetry
│  └─ SystemSnapshot
│
├─ Core
│  ├─ Workloads
│  └─ Validation
│
└─ UI
   ├─ Controls
   ├─ Sections
   ├─ Panels
   └─ Views
```

---

## Current Status

Implemented:

```text
✓ Platform Inventory
✓ CPU Telemetry
✓ GPU Telemetry
✓ Memory Telemetry

✓ CPU Workload
✓ Memory Workload

✓ Validation Engine
✓ Session Tracking
✓ Session History
```

Planned:

```text
□ Qualification Reports
□ JSON Export
□ HTML Export

□ GPU Workload
□ Storage Workload

□ Automated Qualification Runs
```

## Roadmap

### v0.2
- Qualification Reports
- JSON Export

### v0.3
- GPU Workload
- Storage Workload

### v0.4
- Automated Qualification Runs

### v1.0
- Full Platform Qualification Suite


---

## Build

Requirements:

- Windows 11
- .NET 10 SDK

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run
```

Release:

```powershell
dotnet publish `
-c Release `
-r win-x64 `
--self-contained true
```

---

## License

MIT License

Copyright (c) 2026 David Beusing