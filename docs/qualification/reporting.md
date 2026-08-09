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

Both exporters write to a path chosen through the WinForms save dialog. Export is available only after a report has been generated; file-system errors are shown in the UI. Reports contain summary statistics rather than the raw telemetry time series.

