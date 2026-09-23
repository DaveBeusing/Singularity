# Qualification Profiles

Singularity supports three canonical built-in qualification profiles and reusable user-defined custom profiles.

## Identity and ownership

Profile identity is independent from the display name.

Built-in identities are stable:

- `builtin.quick`
- `builtin.standard`
- `builtin.burnin`

Built-in profiles are immutable application defaults. They cannot be edited, deleted, overwritten, or persisted as custom data. Custom profiles receive a stable generated `custom.*` identity. Display names do not need to be unique; two custom profiles can have the same visible name while remaining distinct by identity.

## Validation

A profile must have a non-empty identity and display name, a positive recommended duration, valid percentage values, and coherent threshold relationships.

CPU warning load must not exceed the CPU pass threshold. Memory warning tolerance must not exceed the memory pass tolerance. GPU maximum temperature must be positive and within the supported validation range. GPU warm-up and stability durations cannot be negative, and their combined duration cannot exceed the recommended profile duration.

Invalid profiles cannot be selected for a new qualification run.

## Persistence

Custom profiles are stored per Windows user at:

```text
%LOCALAPPDATA%\Singularity\qualification-profiles.json
```

The file uses an explicit versioned schema and atomic replace-on-write behavior. Built-in profiles are never written to this file.

If persisted custom-profile data has an unsupported schema, duplicate identity, reserved built-in identity, or invalid profile values, loading fails closed. The custom set is not applied, the built-in profiles remain available, and Settings exposes the storage failure.

## Management

Settings provides:

- Create, starting from the Standard profile values;
- Duplicate, for built-in or custom profiles;
- Edit, for custom profiles only;
- Delete, for custom profiles only;
- Delete all custom profiles, which restores a built-in-only catalog.

Deleting or resetting custom profiles never modifies Quick, Standard, or BurnIn.

## Session snapshot semantics

Starting qualification validates the selected profile and copies the complete effective profile into the qualification session. Later edits or deletion of the reusable custom profile cannot change an active or completed session.

Completed evidence stores the full effective profile in addition to its display name. Reports and the persistent qualification archive therefore retain the exact thresholds, durations, GPU warm-up/stability settings, identity, and origin used at execution time.

Legacy archive records from before custom profiles existed resolve their canonical built-in profile from the recorded built-in name. Unknown legacy names fail closed rather than receiving fabricated threshold values.
