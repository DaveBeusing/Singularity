// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application.Commands;
using Xunit;

namespace Singularity.Tests.Application;

public sealed class CommandRouterTests
{
	[Fact]
	public void RegisteredCommandExecutesWhenEnabled()
	{
		CommandRouter router = new();
		int executions = 0;
		router.Register(CommandId.StartQualification, () => executions++);

		bool executed = router.Execute(CommandId.StartQualification);

		Assert.True(executed);
		Assert.Equal(1, executions);
	}

	[Fact]
	public void DisabledCommandDoesNotExecute()
	{
		CommandRouter router = new();
		int executions = 0;
		bool enabled = false;
		router.Register(
			CommandId.StopQualification,
			() => executions++,
			() => enabled);

		Assert.False(router.CanExecute(CommandId.StopQualification));
		Assert.False(router.Execute(CommandId.StopQualification));
		Assert.Equal(0, executions);

		enabled = true;

		Assert.True(router.CanExecute(CommandId.StopQualification));
		Assert.True(router.Execute(CommandId.StopQualification));
		Assert.Equal(1, executions);
	}

	[Fact]
	public void UnknownCommandFailsSafely()
	{
		CommandRouter router = new();

		Assert.False(router.CanExecute(CommandId.ExportJson));
		Assert.False(router.Execute(CommandId.ExportJson));
	}

	[Fact]
	public void RefreshStatesNotifiesBindings()
	{
		CommandRouter router = new();
		int notifications = 0;
		router.StateChanged += () => notifications++;

		router.RefreshStates();

		Assert.Equal(1, notifications);
	}

	[Fact]
	public void DuplicateRegistrationIsRejected()
	{
		CommandRouter router = new();
		router.Register(CommandId.ExportHtml, () => { });

		Assert.Throws<InvalidOperationException>(
			() => router.Register(CommandId.ExportHtml, () => { }));
	}
}
