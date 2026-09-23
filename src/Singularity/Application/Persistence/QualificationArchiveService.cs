// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Singularity.Core.Qualification;
using Singularity.Core.Validation;

namespace Singularity.Application.Persistence;

public sealed class QualificationArchiveService : IDisposable
{
	public const int DefaultRetentionLimit = 100;
	public const string ArchiveFileName = "qualification-archive.json";

	private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

	private readonly object stateLock = new();
	private readonly SemaphoreSlim operationGate = new(1, 1);
	private List<QualificationRecord> records = [];
	private QualificationArchiveState state = QualificationArchiveState.NotLoaded;
	private string? lastError;
	private bool disposed;

	public string ArchivePath { get; }
	public int RetentionLimit { get; }

	public QualificationArchiveState State
	{
		get
		{
			lock (stateLock)
				return state;
		}
	}

	public string? LastError
	{
		get
		{
			lock (stateLock)
				return lastError;
		}
	}

	public IReadOnlyList<QualificationRecord> Records
	{
		get
		{
			lock (stateLock)
				return Array.AsReadOnly(records.ToArray());
		}
	}

	public QualificationArchiveService(
		string? archivePath = null,
		int retentionLimit = DefaultRetentionLimit)
	{
		if (retentionLimit <= 0)
			throw new ArgumentOutOfRangeException(nameof(retentionLimit));

		ArchivePath = archivePath ?? GetDefaultArchivePath();
		RetentionLimit = retentionLimit;
	}

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();
		await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			SetState(QualificationArchiveState.Loading);

			if (!File.Exists(ArchivePath))
			{
				ReplaceRecords([]);
				SetState(QualificationArchiveState.Ready);
				return;
			}

			await using FileStream stream = new(
				ArchivePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				bufferSize: 64 * 1024,
				FileOptions.Asynchronous | FileOptions.SequentialScan);

			QualificationArchiveDocument? document =
				await JsonSerializer.DeserializeAsync<QualificationArchiveDocument>(
					stream,
					SerializerOptions,
					cancellationToken)
					.ConfigureAwait(false);

			if (document is null)
				throw new InvalidDataException("Qualification archive is empty or invalid.");

			if (document.SchemaVersion < QualificationArchiveDocument.MinimumSupportedSchemaVersion ||
				document.SchemaVersion > QualificationArchiveDocument.CurrentSchemaVersion)
			{
				throw new InvalidDataException(
					$"Unsupported qualification archive schema version {document.SchemaVersion}.");
			}

			QualificationRecord[] loadedRecords = document.Records
				.Select(ToValidatedRecord)
				.OrderByDescending(record => record.FinishedAt)
				.ThenByDescending(record => record.StartedAt)
				.Take(RetentionLimit)
				.ToArray();

			ReplaceRecords(loadedRecords);
			SetState(QualificationArchiveState.Ready);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (IsArchiveFailure(ex))
		{
			ReplaceRecords([]);
			SetFailure(ex);
		}
		finally
		{
			operationGate.Release();
		}
	}

	public async Task SaveAsync(
		QualificationRecord record,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);
		ThrowIfDisposed();

		await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			QualificationRecord[] candidateRecords = CreateCandidateRecords(record);
			QualificationArchiveDocument document = new()
			{
				Records = Array.AsReadOnly(
					candidateRecords
						.Select(QualificationArchiveRecordDto.FromRecord)
						.ToArray())
			};

			await WriteDocumentAtomicAsync(document, cancellationToken).ConfigureAwait(false);
			ReplaceRecords(candidateRecords);
			SetState(QualificationArchiveState.Ready);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (IsArchiveFailure(ex))
		{
			SetFailure(ex);
		}
		finally
		{
			operationGate.Release();
		}
	}

	public async Task ClearAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();
		await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (Directory.Exists(ArchivePath))
				throw new IOException("The qualification archive path resolves to a directory.");

			if (File.Exists(ArchivePath))
				File.Delete(ArchivePath);

			ReplaceRecords([]);
			SetState(QualificationArchiveState.Ready);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (IsArchiveFailure(ex))
		{
			SetFailure(ex);
		}
		finally
		{
			operationGate.Release();
		}
	}

	public void Dispose()
	{
		if (disposed)
			return;

		disposed = true;
		operationGate.Dispose();
	}

	private QualificationRecord[] CreateCandidateRecords(QualificationRecord newestRecord)
	{
		QualificationRecord[] existing;
		lock (stateLock)
			existing = records.ToArray();

		return new[] { newestRecord }
			.Concat(existing)
			.GroupBy(CreateRecordKey)
			.Select(group => group.First())
			.OrderByDescending(record => record.FinishedAt)
			.ThenByDescending(record => record.StartedAt)
			.Take(RetentionLimit)
			.ToArray();
	}

	private async Task WriteDocumentAtomicAsync(
		QualificationArchiveDocument document,
		CancellationToken cancellationToken)
	{
		string? directory = Path.GetDirectoryName(ArchivePath);
		if (string.IsNullOrWhiteSpace(directory))
			throw new InvalidDataException("Qualification archive path has no storage directory.");

		Directory.CreateDirectory(directory);

		string temporaryPath =
			$"{ArchivePath}.{Guid.NewGuid():N}.tmp";
		try
		{
			await using (FileStream stream = new(
				temporaryPath,
				FileMode.CreateNew,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 64 * 1024,
				FileOptions.Asynchronous | FileOptions.WriteThrough))
			{
				await JsonSerializer.SerializeAsync(
					stream,
					document,
					SerializerOptions,
					cancellationToken)
					.ConfigureAwait(false);
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
				stream.Flush(flushToDisk: true);
			}

			if (File.Exists(ArchivePath))
				File.Replace(temporaryPath, ArchivePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
			else
				File.Move(temporaryPath, ArchivePath);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				try
				{
					File.Delete(temporaryPath);
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}
	}

	private static QualificationRecord ToValidatedRecord(QualificationArchiveRecordDto dto)
	{
		ArgumentNullException.ThrowIfNull(dto);

		if (dto.StartedAt == default || dto.FinishedAt == default)
			throw new InvalidDataException("Qualification archive contains a record without timestamps.");

		if (dto.FinishedAt < dto.StartedAt)
			throw new InvalidDataException("Qualification archive contains an invalid timestamp range.");

		if (dto.Duration < TimeSpan.Zero)
			throw new InvalidDataException("Qualification archive contains a negative duration.");

		return dto.ToRecord();
	}

	private static QualificationRecordKey CreateRecordKey(QualificationRecord record)
	{
		return new QualificationRecordKey(
			record.StartedAt,
			record.FinishedAt,
			record.ProfileName,
			record.ExecutionMode);
	}

	private static string GetDefaultArchivePath()
	{
		string localApplicationData =
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		if (string.IsNullOrWhiteSpace(localApplicationData))
			throw new InvalidOperationException("Windows local application-data storage is unavailable.");

		return Path.Combine(localApplicationData, "Singularity", ArchiveFileName);
	}

	private static JsonSerializerOptions CreateSerializerOptions()
	{
		JsonSerializerOptions options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = false,
			WriteIndented = true
		};
		options.Converters.Add(new JsonStringEnumConverter());
		return options;
	}

	private static bool IsArchiveFailure(Exception exception)
	{
		return exception is IOException
			or InvalidDataException
			or UnauthorizedAccessException
			or JsonException
			or NotSupportedException
			or ArgumentException;
	}

	private void ReplaceRecords(IEnumerable<QualificationRecord> replacement)
	{
		lock (stateLock)
			records = replacement.ToList();
	}

	private void SetState(QualificationArchiveState value)
	{
		lock (stateLock)
		{
			state = value;
			lastError = null;
		}
	}

	private void SetFailure(Exception exception)
	{
		lock (stateLock)
		{
			state = QualificationArchiveState.Failed;
			lastError = exception.Message;
		}
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(disposed, this);
	}

	private readonly record struct QualificationRecordKey(
		DateTime StartedAt,
		DateTime FinishedAt,
		string ProfileName,
		QualificationExecutionMode ExecutionMode);
}
