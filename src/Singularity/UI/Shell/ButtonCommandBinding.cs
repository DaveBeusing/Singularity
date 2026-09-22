// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Commands;

namespace Singularity.UI.Shell;

public sealed class ButtonCommandBinding : IDisposable
{
	private readonly Button button;
	private readonly CommandRouter commandRouter;
	private readonly CommandId commandId;
	private bool disposed;

	public ButtonCommandBinding(
		Button button,
		CommandRouter commandRouter,
		CommandId commandId)
	{
		this.button = button ?? throw new ArgumentNullException(nameof(button));
		this.commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
		this.commandId = commandId;

		button.Click += OnClick;
		commandRouter.StateChanged += OnCommandStateChanged;
		UpdateEnabledState();
	}

	private void OnClick(object? sender, EventArgs e)
	{
		commandRouter.Execute(commandId);
		UpdateEnabledState();
	}

	private void OnCommandStateChanged()
	{
		if (!disposed && !button.IsDisposed)
			UpdateEnabledState();
	}

	private void UpdateEnabledState()
	{
		button.Enabled = commandRouter.CanExecute(commandId);
	}

	public void Dispose()
	{
		if (disposed)
			return;

		disposed = true;
		button.Click -= OnClick;
		commandRouter.StateChanged -= OnCommandStateChanged;
	}
}
