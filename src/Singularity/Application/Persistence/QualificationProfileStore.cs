// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using Singularity.Core.Validation;

namespace Singularity.Application.Persistence;

public sealed class QualificationProfileStore : IDisposable
{
	public const string ProfileFileName = "qualification-profiles.json";

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = false,
		WriteIndented = true
	};

	private readonly SemaphoreSlim operationGate = new(1, 1);
	private readonly object stateLock = new();
	private List<QualificationProfile> profiles = [];
	private QualificationProfileStoreState state = QualificationProfileStoreState.NotLoaded;
	private string? lastError;
	private bool disposed;

	public string StoragePath { get; }
	public QualificationProfileStoreState State { get { lock (stateLock) return state; } }
	public string? LastError { get { lock (stateLock) return lastError; } }
	public IReadOnlyList<QualificationProfile> Profiles
	{
		get { lock (stateLock) return Array.AsReadOnly(profiles.Select(p => p.Snapshot()).ToArray()); }
	}

	public QualificationProfileStore(string? storagePath = null)
	{
		StoragePath = storagePath ?? GetDefaultPath();
	}

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();
		await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			SetState(QualificationProfileStoreState.Loading);
			if (!File.Exists(StoragePath))
			{
				ReplaceProfiles([]);
				SetState(QualificationProfileStoreState.Ready);
				return;
			}

			await using FileStream stream = new(
				StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read,
				64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
			QualificationProfileDocument? document =
				await JsonSerializer.DeserializeAsync<QualificationProfileDocument>(
					stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
			if (document is null)
				throw new InvalidDataException("Qualification profile storage is empty or invalid.");
			if (document.SchemaVersion != QualificationProfileDocument.CurrentSchemaVersion)
				throw new InvalidDataException(
					$"Unsupported qualification profile schema version {document.SchemaVersion}.");

			QualificationProfile[] loaded = document.Profiles.Select(dto => dto.ToProfile()).ToArray();
			ValidateCollection(loaded);
			ReplaceProfiles(loaded);
			SetState(QualificationProfileStoreState.Ready);
		}
		catch (OperationCanceledException) { throw; }
		catch (Exception ex) when (IsStorageFailure(ex))
		{
			ReplaceProfiles([]);
			SetFailure(ex);
		}
		finally { operationGate.Release(); }
	}

	public async Task SaveAsync(
		IReadOnlyList<QualificationProfile> customProfiles,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(customProfiles);
		ThrowIfDisposed();
		QualificationProfile[] snapshots = customProfiles.Select(p => p.Snapshot()).ToArray();
		ValidateCollection(snapshots);

		await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			QualificationProfileDocument document = new()
			{
				Profiles = Array.AsReadOnly(snapshots.Select(QualificationProfileDto.FromProfile).ToArray())
			};
			await WriteAtomicAsync(document, cancellationToken).ConfigureAwait(false);
			ReplaceProfiles(snapshots);
			SetState(QualificationProfileStoreState.Ready);
		}
		catch (OperationCanceledException) { throw; }
		catch (Exception ex) when (IsStorageFailure(ex)) { SetFailure(ex); }
		finally { operationGate.Release(); }
	}

	public void Dispose()
	{
		if (disposed) return;
		disposed = true;
		operationGate.Dispose();
	}

	private async Task WriteAtomicAsync(QualificationProfileDocument document, CancellationToken cancellationToken)
	{
		string? directory = Path.GetDirectoryName(StoragePath);
		if (string.IsNullOrWhiteSpace(directory))
			throw new InvalidDataException("Qualification profile path has no storage directory.");
		Directory.CreateDirectory(directory);
		string temporaryPath = $"{StoragePath}.{Guid.NewGuid():N}.tmp";
		try
		{
			await using (FileStream stream = new(
				temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
				64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
			{
				await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken)
					.ConfigureAwait(false);
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
				stream.Flush(flushToDisk: true);
			}
			if (File.Exists(StoragePath))
				File.Replace(temporaryPath, StoragePath, null, ignoreMetadataErrors: true);
			else
				File.Move(temporaryPath, StoragePath);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				try { File.Delete(temporaryPath); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
		}
	}

	private static void ValidateCollection(IReadOnlyList<QualificationProfile> candidateProfiles)
	{
		HashSet<string> ids = new(StringComparer.Ordinal);
		foreach (QualificationProfile profile in candidateProfiles)
		{
			QualificationProfileValidator.EnsureValid(profile);
			if (profile.Origin != QualificationProfileOrigin.Custom ||
				QualificationProfiles.IsBuiltInId(profile.Id))
				throw new InvalidDataException("Only custom qualification profiles may be persisted.");
			if (!ids.Add(profile.Id))
				throw new InvalidDataException($"Duplicate qualification profile identity '{profile.Id}'.");
		}
	}

	private static string GetDefaultPath()
	{
		string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (string.IsNullOrWhiteSpace(local))
			throw new InvalidOperationException("Windows local application-data storage is unavailable.");
		return Path.Combine(local, "Singularity", ProfileFileName);
	}

	private static bool IsStorageFailure(Exception exception) =>
		exception is IOException or InvalidDataException or UnauthorizedAccessException
			or JsonException or NotSupportedException or ArgumentException;

	private void ReplaceProfiles(IEnumerable<QualificationProfile> replacement)
	{
		lock (stateLock) profiles = replacement.Select(p => p.Snapshot()).ToList();
	}

	private void SetState(QualificationProfileStoreState value)
	{
		lock (stateLock) { state = value; lastError = null; }
	}

	private void SetFailure(Exception exception)
	{
		lock (stateLock) { state = QualificationProfileStoreState.Failed; lastError = exception.Message; }
	}

	private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
