// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Application.Commands;

public sealed class CommandRouter
{
	private readonly Dictionary<CommandId, ApplicationCommand> commands = [];

	public event Action? StateChanged;

	public void Register(
		CommandId id,
		Action execute,
		Func<bool>? canExecute = null)
	{
		if (!commands.TryAdd(id, new ApplicationCommand(execute, canExecute)))
			throw new InvalidOperationException($"Command '{id}' is already registered.");
	}

	public bool IsRegistered(CommandId id) =>
		commands.ContainsKey(id);

	public bool CanExecute(CommandId id) =>
		commands.TryGetValue(id, out ApplicationCommand? command) &&
		command.CanExecute;

	public bool Execute(CommandId id) =>
		commands.TryGetValue(id, out ApplicationCommand? command) &&
		command.TryExecute();

	public void RefreshStates()
	{
		StateChanged?.Invoke();
	}
}
