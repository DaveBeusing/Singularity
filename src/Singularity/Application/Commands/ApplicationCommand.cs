// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

namespace Singularity.Application.Commands;

public sealed class ApplicationCommand
{
	private readonly Action execute;
	private readonly Func<bool> canExecute;

	public ApplicationCommand(Action execute, Func<bool>? canExecute = null)
	{
		this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
		this.canExecute = canExecute ?? (() => true);
	}

	public bool CanExecute => canExecute();

	public bool TryExecute()
	{
		if (!CanExecute)
			return false;

		execute();
		return true;
	}
}
