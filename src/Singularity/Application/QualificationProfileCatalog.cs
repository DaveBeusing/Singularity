// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Persistence;
using Singularity.Core.Validation;

namespace Singularity.Application;

public sealed class QualificationProfileCatalog
{
	private readonly QualificationProfileStore store;
	private List<QualificationProfile> customProfiles = [];

	public QualificationProfileStoreState State => store.State;
	public string? LastError => store.LastError;
	public string StoragePath => store.StoragePath;
	public IReadOnlyList<QualificationProfile> Profiles =>
		Array.AsReadOnly(
			QualificationProfiles.All
				.Concat(customProfiles)
				.Select(profile => profile.Snapshot())
				.ToArray());

	public QualificationProfileCatalog(QualificationProfileStore store)
	{
		this.store = store ?? throw new ArgumentNullException(nameof(store));
	}

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		await store.LoadAsync(cancellationToken).ConfigureAwait(false);
		customProfiles = store.State == QualificationProfileStoreState.Ready
			? store.Profiles.Select(profile => profile.Snapshot()).ToList()
			: [];
	}

	public async Task<QualificationProfile?> CreateAsync(
		QualificationProfile template,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(template);
		QualificationProfile profile = template with
		{
			Id = $"custom.{Guid.NewGuid():N}",
			Origin = QualificationProfileOrigin.Custom
		};
		QualificationProfileValidator.EnsureValid(profile);

		List<QualificationProfile> candidate = customProfiles.Select(p => p.Snapshot()).ToList();
		candidate.Add(profile);
		if (!await PersistAsync(candidate, cancellationToken).ConfigureAwait(false))
			return null;
		return profile.Snapshot();
	}

	public Task<QualificationProfile?> DuplicateAsync(
		QualificationProfile source,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		return CreateAsync(
			source with { Name = $"{source.Name} Copy" },
			cancellationToken);
	}

	public async Task<bool> UpdateAsync(
		QualificationProfile profile,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(profile);
		if (profile.IsBuiltIn || QualificationProfiles.IsBuiltInId(profile.Id))
			return false;

		int index = customProfiles.FindIndex(
			item => string.Equals(item.Id, profile.Id, StringComparison.Ordinal));
		if (index < 0)
			return false;

		QualificationProfile replacement = profile with { Origin = QualificationProfileOrigin.Custom };
		QualificationProfileValidator.EnsureValid(replacement);
		List<QualificationProfile> candidate = customProfiles.Select(p => p.Snapshot()).ToList();
		candidate[index] = replacement;
		return await PersistAsync(candidate, cancellationToken).ConfigureAwait(false);
	}

	public async Task<bool> DeleteAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(id) || QualificationProfiles.IsBuiltInId(id))
			return false;
		List<QualificationProfile> candidate = customProfiles
			.Where(profile => !string.Equals(profile.Id, id, StringComparison.Ordinal))
			.Select(profile => profile.Snapshot())
			.ToList();
		if (candidate.Count == customProfiles.Count)
			return false;
		return await PersistAsync(candidate, cancellationToken).ConfigureAwait(false);
	}

	public Task<bool> ResetCustomAsync(CancellationToken cancellationToken = default) =>
		PersistAsync([], cancellationToken);

	private async Task<bool> PersistAsync(
		IReadOnlyList<QualificationProfile> candidate,
		CancellationToken cancellationToken)
	{
		await store.SaveAsync(candidate, cancellationToken).ConfigureAwait(false);
		if (store.State != QualificationProfileStoreState.Ready)
			return false;
		customProfiles = store.Profiles.Select(profile => profile.Snapshot()).ToList();
		return true;
	}
}
