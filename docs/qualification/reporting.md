# Reporting

Reporting begins when a qualification session is finalized and a last validation result is available. `QualificationReportGenerator` requires both session timestamps and maps the session profile, duration, final component results, overall result, frozen streaming telemetry statistics, bounded telemetry timeline, and per-device GPU evidence into a `QualificationReport`.

## Streaming statistics

During a running session, snapshots continue to feed the existing bounded streaming metric accumulators. Each available metric records sample count, minimum, average, and maximum without retaining every raw sample. Non-finite values are ignored.

CPU load and system-memory utilization remain session-level statistics. For every explicitly selected GPU, the session independently records:

- stable device identifier and last known name;
- available and unavailable sample counts;
- GPU load;
- GPU temperature;
- GPU power when available;
- GPU VRAM usage.

Single-GPU sessions also populate the previous aggregate GPU statistic fields so existing single-device evidence remains understandable. Multi-GPU sessions deliberately avoid fabricating aggregate GPU statistics; their `Gpus` collection is the authoritative evidence.

## Bounded telemetry timeline

Qualification sessions additionally retain a bounded chronological evidence timeline so Results and Reports can explain when telemetry changed during a run.

The timeline is evidence-oriented rather than a raw telemetry log:

- at most 720 telemetry points are frozen per completed session;
- the initial sampling interval is derived from the selected qualification profile and is never faster than 500 ms;
- if a run exceeds its expected duration, the collector deterministically coarsens the retained series by increasing the sampling interval;
- start/end evidence and observed metric extrema are retained when the frozen budget is assembled;
- unavailable GPU telemetry is represented by nullable metric values and therefore renders as a chart gap rather than a numeric zero;
- CPU load and system-memory utilization are stored at session level;
- GPU load, temperature, power, and VRAM utilization remain associated with the stable GPU identifier;
- event markers are bounded separately and include qualification start, automated step transitions, explicit stop/cancel actions, completion, and failure where applicable.

The timeline is frozen only when the qualification session completes or fails. The existing streaming statistics remain the authoritative summary statistics and are not recalculated from the downsampled timeline.

## JSON export

`QualificationJsonExporter` writes indented, camel-case JSON and serializes enum values as names.

Schema version `3.0` preserves the schema-`2.0` multi-GPU evidence contract and adds the top-level `telemetryTimeline` object. The timeline contains:

- its own timeline schema version;
- configured maximum point count;
- effective sampling interval;
- bounded chronological telemetry points;
- nullable metric values for unavailable evidence;
- bounded qualification event markers.

Every `gpuEvidence` entry continues to contain:

- the stable GPU identifier;
- device name;
- per-device validation result and message;
- explicit telemetry availability;
- available and unavailable sample counts;
- load, temperature, power, and VRAM statistics.

The scalar `validation.gpu` value remains the deterministic aggregate GPU result. Existing session-level telemetry fields also remain present; single-GPU runs continue to populate their legacy GPU metrics. Consumers that require time-based evidence should use schema `3.0` and read `telemetryTimeline`; consumers that require unambiguous multi-device evidence should continue to read `gpuEvidence`.

Hardware inventory remains a separate summary of the machine and is not a substitute for qualification evidence. Stable GPU identifiers appear in inventory, qualification evidence, and timeline points so consumers can correlate the exercised devices explicitly.

## HTML export

`QualificationHtmlExporter` uses the same intermediate document as JSON export. It produces a standalone responsive dark-theme HTML file with:

- session and profile details;
- aggregate CPU, memory, GPU, and overall validation cards;
- one GPU evidence card per selected device;
- stable device identity and validation message;
- per-device telemetry statistics and availability gaps;
- session-level streaming telemetry statistics;
- bounded telemetry timeline charts for system utilization and available per-GPU load, temperature, power, and VRAM metrics;
- qualification event markers;
- hardware inventory;
- schema version and generation timestamp.

Timeline charts are rendered as embedded SVG. Missing values break the affected series instead of being plotted as zero. The report has no external web, script, charting, or font dependency.

Both exporters write to a path chosen through the WinForms save dialog. `ReportExportService` supplies the same assembly-derived application version displayed by the UI. Export is available only after a report has been generated; file-system errors are shown in the UI.

## Results and history evidence

The Results workspace presents the newest completed qualification record rather than the mutable currently configured profile. Each bounded history record retains:

- start and finish time;
- duration;
- final result;
- qualification profile name;
- manual or automated execution mode;
- frozen streaming telemetry statistics;
- frozen bounded telemetry timeline;
- frozen per-device GPU evidence;
- the generated qualification report when one is available.

Results renders the bounded timeline using lightweight WinForms drawing without an external charting dependency. Chart controls render frozen evidence only and do not introduce their own telemetry polling or continuous invalidation. A failed session can remain visible even when no exportable report exists: any frozen timeline/statistical evidence remains attached to the session while unavailable validation evidence stays `UNKNOWN` rather than being fabricated.

`QualificationHistory` remains capped at ten records for the live runtime model. Completed records are also written to the local versioned qualification archive, which retains up to 100 records by default and is loaded asynchronously at startup. Results and Reports use the resulting bounded archive-backed evidence list.

## Reports workspace

Reports presents the bounded archive-backed evidence list as selectable entries. Selection drives the central report preview and inspector. A newly completed session becomes the current selection, while persisted records remain selectable after application restart until archive retention removes them or the user explicitly clears the archive.

The report preview shows the same frozen telemetry timeline and per-device GPU evidence as Results. JSON and HTML export operate on the selected history record's existing `QualificationReport`. Export commands remain disabled when the selected record has no report evidence or platform inventory is unavailable. Export does not create a second report model and does not add a web server or external web dependency.

## Persistent qualification archive

Completed evidence is serialized through the versioned application-layer archive contract and stored per Windows user at `%LOCALAPPDATA%\Singularity\qualification-archive.json`. Archive schema `2` adds the bounded telemetry timeline while retaining read compatibility with schema `1`; schema-`1` records load with an empty timeline. Writes use a same-directory temporary file followed by atomic replacement so a failed new write does not destroy the prior valid archive. Corrupt data, unsupported schema versions, permission failures, and I/O errors fail closed and surface an archive failure state without crashing startup. See [Qualification archive](qualification-archive.md) for schema ownership, retention, recovery, deletion, and privacy behavior.
