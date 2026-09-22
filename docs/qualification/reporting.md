# Reporting

Reporting begins when a qualification session is finalized and a last validation result is available. `QualificationReportGenerator` requires both session timestamps and maps the session profile, duration, final component results, overall result, and frozen telemetry statistics into a `QualificationReport`.

## Statistics

During a running session, snapshots feed streaming metric accumulators. Each available metric records sample count, minimum, average, and maximum without retaining every raw sample. Non-finite values are ignored. The report includes statistics for:

- CPU load;
- GPU load;
- GPU temperature;
- GPU power;
- GPU VRAM usage;
- system memory usage.

A metric is absent when it received no usable samples.

## JSON export

`QualificationJsonExporter` writes indented, camel-case JSON and serializes enum values as names. Schema version `1.0` contains:

- Singularity version and report timestamp;
- session duration and complete qualification profile;
- CPU, memory, GPU, and overall validation results;
- session telemetry statistics;
- an inventory summary for the operating system, computer, mainboard, processor, GPUs, memory modules, and storage drives.

## HTML export

`QualificationHtmlExporter` uses the same intermediate document as JSON export. It produces a standalone responsive dark-theme HTML file containing session and profile details, validation cards, a telemetry table, hardware inventory, schema version, and generation timestamp. Dynamic text is HTML encoded.

Both exporters write to a path chosen through the WinForms save dialog. `ReportExportService` supplies the same assembly-derived application version displayed by the UI. Export is available only after a report has been generated; file-system errors are shown in the UI. Reports contain summary statistics rather than the raw telemetry time series.

## Results and history evidence

The Results workspace presents the newest completed qualification record rather than the mutable currently configured profile. Each bounded history record retains:

- start and finish time;
- duration;
- final result;
- qualification profile name;
- manual or automated execution mode;
- frozen session telemetry statistics;
- the generated qualification report when one is available.

The history remains in memory only and is capped at ten records. A failed session can therefore remain visible in Results even when no exportable report exists; missing component validation evidence stays `UNKNOWN`/unavailable rather than being fabricated.

## Reports workspace

Reports presents the bounded history as selectable entries. Selection drives the central report preview and inspector. A newly completed session becomes the current selection, while older records remain selectable for the lifetime of the application session.

JSON and HTML export operate on the selected history record's existing `QualificationReport`. Export commands remain disabled when the selected record has no report evidence or platform inventory is unavailable. Export does not create a second report model and does not add a web server or external web dependency.
