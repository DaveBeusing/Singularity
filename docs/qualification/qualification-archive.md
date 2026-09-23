# Qualification Archive

Singularity persists completed qualification evidence locally so Results and Reports remain useful after the application restarts. Persistence is owned by the application/infrastructure layer; `Core/Validation` remains independent from file-system concerns.

## Storage location

The default per-user archive is:

```text
%LOCALAPPDATA%\Singularity\qualification-archive.json
```

The archive is local to the current Windows user. Singularity does not sync this data to cloud or network storage.

## Schema and ownership

`Application/Persistence/QualificationArchiveDocument` owns the persistence contract. The current schema version is `1`.

Each persisted record contains the completed-session timestamps, duration, final result, execution mode, profile name, frozen session telemetry statistics, per-device GPU evidence, and the generated qualification report when available. Enum values are written as readable names. Unsupported schema versions fail closed rather than being interpreted approximately.

The archive stores completed evidence only. It does not persist raw telemetry time series.

## Retention

Two limits intentionally serve different purposes:

- `QualificationHistory` retains the ten newest records for the live qualification runtime model.
- `QualificationArchiveService` retains up to 100 completed records by default for restart-safe Results/Reports evidence.

The coordinator exposes the bounded archive-backed evidence list to Results and Reports while preserving the existing ten-entry live history.

## Startup behavior

Archive loading begins from the WinForms `Shown` lifecycle and uses asynchronous file I/O. Platform inventory loading runs concurrently. The UI remains available while the archive is loading.

Archive state is explicit:

- `NotLoaded`
- `Loading`
- `Ready`
- `Failed`

Settings displays the current archive state, stored-record count, storage path, and any recoverable archive error.

## Atomic writes and recovery

Every completed qualification record is queued for persistence without blocking the WinForms UI thread.

Writes use this sequence:

1. build the next bounded archive document in memory;
2. serialize to a uniquely named temporary file in the same directory;
3. flush the temporary file;
4. atomically replace the existing archive, or move the temporary file into place for the first write;
5. update in-memory archive state only after the replacement succeeds.

If serialization, permission, sharing, or I/O failure prevents the replacement, the previous valid archive remains untouched. Temporary files are removed on the best-effort cleanup path. The failed write is surfaced through archive state rather than crashing the application.

Corrupted JSON, invalid record timestamps, unsupported schema versions, permission failures, and ordinary I/O failures produce a recoverable `Failed` state. They do not block application startup or fabricate evidence.

## Clear/delete behavior

Settings contains an explicit **Clear qualification archive** action. The action requires confirmation. A successful clear deletes the persisted archive and clears the current Results/Reports evidence list and live history.

If deletion fails, the archive enters `Failed` state and the existing evidence remains available rather than being silently discarded.

## Shutdown behavior

If an archive write is still pending when the main window is closed, Singularity waits asynchronously for that write to complete before final disposal. This prevents normal application shutdown from racing the final completed-session write.

## Privacy

The qualification archive can contain hardware identity, qualification outcomes, timestamps, telemetry statistics, and report evidence. It is stored only in the current user's local application-data directory unless a test or future explicitly configured storage path is used. Clearing the archive removes Singularity's local persisted qualification evidence file.
