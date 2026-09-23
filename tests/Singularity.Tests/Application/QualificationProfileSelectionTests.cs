// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Application;
using Singularity.Core.Validation;

namespace Singularity.Tests.Application;

public sealed class QualificationProfileSelectionTests
{
	[Fact]
	public void Workspace_SelectsCustomProfileByStableIdentity()
	{
		QualificationProfile custom = QualificationProfiles.Standard with
		{
			Id = "custom.lab",
			Name = "Lab",
			Origin = QualificationProfileOrigin.Custom,
			CpuMinimumLoadPercent = 88
		};
		QualificationWorkspaceState state = new();
		state.SetAvailableProfiles([.. QualificationProfiles.All, custom]);

		state.SetConfiguration(
			QualificationConfiguration.Default with { Profile = custom });

		Assert.Null(state.ValidateConfiguration());
		Assert.Equal("custom.lab", state.Configuration.Profile.Id);
		Assert.Equal(88, state.Configuration.Profile.CpuMinimumLoadPercent);
		Assert.Contains(state.AvailableProfiles, profile => profile.Id == custom.Id);
	}

	[Fact]
	public void Workspace_WhenSelectedCustomProfileIsRemoved_FallsBackToStandard()
	{
		QualificationProfile custom = QualificationProfiles.Standard with
		{
			Id = "custom.removed",
			Name = "Removed",
			Origin = QualificationProfileOrigin.Custom
		};
		QualificationWorkspaceState state = new();
		state.SetAvailableProfiles([.. QualificationProfiles.All, custom]);
		state.SetConfiguration(
			QualificationConfiguration.Default with { Profile = custom });

		state.SetAvailableProfiles(QualificationProfiles.All);

		Assert.Equal(QualificationProfiles.Standard.Id, state.Configuration.Profile.Id);
		Assert.Null(state.ValidateConfiguration());
	}
}
