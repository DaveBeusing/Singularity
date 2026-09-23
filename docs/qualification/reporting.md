# Reporting

Reporting begins when a qualification session is finalized and a last validation result is available. `QualificationReportGenerator` requires both session timestamps and maps the session profile, duration, final component results, overall result, frozen telemetry statistics, and per-device GPU evidence into a `QualificationReport`.

## Statistics

During a running session, snapshots feed bounded streaming metric accumulators. Each available metric records sample count, minimum, average, and maximum without retaining every raw sample. Non-finite values are ignored.

CPU load and system-memory utilization remain session-level statistics. For every explicitly selected GPU, the session independently records:

- stable device identifier and last known name;
- available and unavailable sample counts;
- GPU load;
- GPU temperature;
- GPU power when available;
- GPU VRAM usage.

Single-GPU sessions also populate the previous aggregate GPU statistic fields so existing single-device evidence remains understandable. Multi-GPU sessions deliberately avoid fabricating aggregate GPU statistics; their `Gpus` collection is the authoritative evidence.

## JSON export

`QualificationJsonExporter` writes indented, camel-case JSON and serializes enum values as names.

Schema version `2.0` adds the top-level `gpuEvidence` collection. Every entry contains:

- the stable GPU identifier;
- device name;
- per-device validation result and message;
- explicit telemetry availability;
- available and unavailable sample counts;
- load, temperature, power, and VRAM statistics.

The existing scalar `validation.gpu` value remains as the deterministic aggregate GPU result. Existing session-level telemetry fields also remain present; single-GPU runs continue to populate their legacy GPU metrics. Schema `2.0` is intentionally versioned because consumers that need unambiguous multi-GPU evidence must read `gpuEvidence`. A schema-`1.0` consumer must not infer multi-device identity from the scalar GPU fields.

Hardware inventory remains a separate summary of the machine and is not a substitute for qualification evidence. Stable GPU identifiers appear in both inventory and qualification evidence so consumers can correlate the exercised devices explicitly.

## HTML export

`QualificationHtmlExporter` uses the same intermediate document as JSON export. It produces a standalone responsive dark-theme HTML file with:

- session and profile details;
- aggregate CPU, memory, GPU, and overall validation cards;
- one GPU evidence card per selected device;
- stable device identity and validation message;
- per-device telemetry statistics and availability gaps;
- session-level telemetry;
- hardware inventory;
- schema version and generation timestamp.

Dynamic text is HTML encoded and the report has no external web dependency.

Both exporters write to a path chosen through the WinForms save dialog. `ReportExportService` supplies the same assembly-derived application version displayed by the UI. Export is available only after a report has been generated; file-system errors are shown in the UI. Reports contain summary statistics rather than the raw telemetry time series.

## Results and history evidence

The Results workspace presents the newest completed qualification record rather than the mutable currently configured profile. Each bounded history record retains:

- start and finish time;
- duration;
- final result;
- qualification profile name;
- manual or automated execution mode;
- frozen session telemetry statistics;
- frozen per-device GPU evidence;
- the generated qualification report when one is available.

Results renders a separate evidence card for every selected GPU. A failed session can remain visible even when no exportable report exists: any frozen GPU statistics remain attached to their stable device identifiers while unavailable validation evidence stays `UNKNOWN` rather than being fabricated.

The history remains in memory only and is capped at ten records.

## Reports workspace

Reports presents the bounded history as selectable entries. Selection drives the central report preview and inspector. A newly completed session becomes the current selection, while older records remain selectable for the lifetime of the application session.

The report preview shows per-device GPU evidence in addition to the aggregate component status. JSON and HTML export operate on the selected history record's existing `QualificationReport`. Export commands remain disabled when the selected record has no report evidence or platform inventory is unavailable. Export does not create a second report model and does not add a web server or external web dependency.
