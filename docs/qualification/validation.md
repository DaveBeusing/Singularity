# Validation

`WorkloadValidator` compares the active `WorkloadStatus` and latest `SystemSnapshot` against a `QualificationProfile`. It returns independent CPU, memory, and GPU statuses plus a human-readable message for each.

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

GPU load must remain at or above the minimum for the profile's stability duration before passing. Dropping below the minimum resets that stability window. Validator state is reset when a new session begins.

`ValidationSummary` determines the overall result by precedence: any failure wins, otherwise any warning, otherwise any pass, otherwise unknown. `ValidationResult.IsSuccess` treats warnings and unknown components as non-failures; this is the value used by automated stop-on-failure behavior.

`QualificationSession` records the chosen profile, timestamps, final overall result, and telemetry samples. Completed sessions are added newest-first to an in-memory history limited to ten records; history is not persisted between application runs.

