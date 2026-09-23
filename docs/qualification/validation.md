# Validation

`WorkloadValidator` compares the active `WorkloadStatus` and latest `SystemSnapshot` against a `QualificationProfile`. It returns independent CPU and memory results plus one `GpuValidationResult` for every explicitly selected GPU. The scalar GPU result is derived deterministically from those per-device results.

## Profiles

| Profile | Duration | CPU pass / warning | Memory pass / warning | GPU minimum | GPU maximum | Warm-up | Stable load |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Quick | 5 min | 75% / 45% | 85% / 70% | 75% | 90 °C | 5 s | 2 s |
| Standard | 15 min | 80% / 50% | 90% / 75% | 85% | 85 °C | 10 s | 3 s |
| BurnIn | 1 hour | 90% / 70% | 95% / 85% | 90% | 80 °C | 20 s | 5 s |

Memory percentages describe the required fraction of the requested allocation. For example, Standard passes at 90% of requested memory and warns from 75% up to that pass threshold.

## Status rules

- `Pass` means the enabled component meets its profile threshold.
- `Warning` means CPU or memory is between warning and pass thresholds, or GPU monitoring is initializing, warming up, unavailable, or waiting for stable load.
- `Fail` means CPU or memory is below its warning threshold, GPU temperature exceeds the profile maximum, or post-warm-up GPU load is below its minimum.
- `Unknown` means that workload component is disabled.

For every explicitly selected GPU, `WorkloadValidator` resolves the matching `GpuTelemetrySnapshot`. Missing or unavailable telemetry remains `UNKNOWN` for that device; telemetry from another enumerated GPU is never substituted. Because an enabled GPU workload cannot be qualified as passing without evidence, the derived aggregate GPU status becomes `WARNING` whenever any selected device is `UNKNOWN` and no device has failed. Each GPU owns an independent warm-up and stable-load timer so a slow or interrupted device cannot inherit another adapter's stability state. Legacy callers without explicit identifiers retain the compatibility behavior based on the first GPU fields in `SystemSnapshot`.

GPU load must remain at or above the minimum for the profile's stability duration before that device passes. Dropping below the minimum resets only that device's stability window. Validator state is reset when a new session begins.

Per-device GPU status is aggregated with the same deterministic precedence used elsewhere: any failure wins, otherwise any warning, otherwise any pass, otherwise unknown. `ValidationSummary` then derives the overall qualification result from CPU, memory, and the derived GPU result with that precedence. `ValidationResult.IsSuccess` treats warnings and unknown components as non-failures; this remains the value used by automated stop-on-failure behavior.

`QualificationSession` records the chosen profile, timestamps, final overall result, selected stable GPU identities, and bounded streaming telemetry statistics. Completed sessions freeze per-GPU evidence into the newest-first in-memory history limited to ten records; history is not persisted between application runs.

