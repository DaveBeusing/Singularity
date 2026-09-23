// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core.Validation;
using Singularity.UI.Controls;

namespace Singularity.UI.Views;

public sealed class QualificationProfileEditorDialog : Form
{
	private readonly QualificationProfile source;
	private readonly TextBox nameInput = new();
	private readonly NumericUpDown durationMinutes = CreateNumeric(1, 525600, 0);
	private readonly NumericUpDown cpuMinimum = CreateNumeric(0, 100, 1);
	private readonly NumericUpDown cpuWarning = CreateNumeric(0, 100, 1);
	private readonly NumericUpDown memoryPass = CreateNumeric(0, 100, 1);
	private readonly NumericUpDown memoryWarning = CreateNumeric(0, 100, 1);
	private readonly NumericUpDown gpuMinimum = CreateNumeric(0, 100, 1);
	private readonly NumericUpDown gpuTemperature = CreateNumeric(1, 150, 1);
	private readonly NumericUpDown gpuWarmupSeconds = CreateNumeric(0, 86400, 0);
	private readonly NumericUpDown gpuStabilitySeconds = CreateNumeric(0, 86400, 0);

	public QualificationProfile ResultProfile { get; private set; }

	public QualificationProfileEditorDialog(QualificationProfile profile, string title)
	{
		source = profile ?? throw new ArgumentNullException(nameof(profile));
		ResultProfile = profile.Snapshot();
		Text = title;
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		BackColor = Theme.ApplicationBackground;
		ForeColor = Theme.TextMain;
		Font = ThemeFonts.CardText;
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(620, 590);
		BuildUi();
		LoadProfile(profile);
	}

	private void BuildUi()
	{
		TableLayoutPanel grid = new()
		{
			Dock = DockStyle.Fill, Padding = new Padding(ThemeMetrics.SpacingLarge),
			ColumnCount = 2, RowCount = 12, BackColor = Theme.ApplicationBackground
		};
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
		grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
		for (int row = 0; row < 11; row++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
		grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		nameInput.Dock = DockStyle.Fill; nameInput.BackColor = Theme.Panel; nameInput.ForeColor = Theme.TextMain;
		AddRow(grid, 0, "Name", nameInput);
		AddRow(grid, 1, "Recommended duration (minutes)", durationMinutes);
		AddRow(grid, 2, "CPU pass load (%)", cpuMinimum);
		AddRow(grid, 3, "CPU warning load (%)", cpuWarning);
		AddRow(grid, 4, "Memory pass tolerance (%)", memoryPass);
		AddRow(grid, 5, "Memory warning tolerance (%)", memoryWarning);
		AddRow(grid, 6, "GPU pass load (%)", gpuMinimum);
		AddRow(grid, 7, "GPU maximum temperature (°C)", gpuTemperature);
		AddRow(grid, 8, "GPU warm-up (seconds)", gpuWarmupSeconds);
		AddRow(grid, 9, "GPU stability (seconds)", gpuStabilitySeconds);

		FlowLayoutPanel actions = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.ApplicationBackground };
		CommandButton save = new() { Text = "Save", Width = 110, Height = ThemeMetrics.ControlHeight };
		CommandButton cancel = new() { Text = "Cancel", Width = 110, Height = ThemeMetrics.ControlHeight, DialogResult = DialogResult.Cancel };
		save.Click += (_, _) => Save();
		actions.Controls.Add(save); actions.Controls.Add(cancel);
		grid.Controls.Add(actions, 0, 11); grid.SetColumnSpan(actions, 2);
		AcceptButton = save; CancelButton = cancel;
		Controls.Add(grid);
	}

	private void LoadProfile(QualificationProfile profile)
	{
		nameInput.Text = profile.Name;
		durationMinutes.Value = Clamp((decimal)profile.RecommendedDuration.TotalMinutes, durationMinutes);
		cpuMinimum.Value = Clamp((decimal)profile.CpuMinimumLoadPercent, cpuMinimum);
		cpuWarning.Value = Clamp((decimal)profile.CpuWarningLoadPercent, cpuWarning);
		memoryPass.Value = Clamp((decimal)profile.MemoryAllocationTolerancePercent, memoryPass);
		memoryWarning.Value = Clamp((decimal)profile.MemoryWarningTolerancePercent, memoryWarning);
		gpuMinimum.Value = Clamp((decimal)profile.GpuMinimumLoadPercent, gpuMinimum);
		gpuTemperature.Value = Clamp((decimal)profile.GpuMaximumTemperatureCelsius, gpuTemperature);
		gpuWarmupSeconds.Value = Clamp((decimal)profile.GpuWarmupDuration.TotalSeconds, gpuWarmupSeconds);
		gpuStabilitySeconds.Value = Clamp((decimal)profile.GpuStabilityDuration.TotalSeconds, gpuStabilitySeconds);
	}

	private void Save()
	{
		QualificationProfile candidate = new(
			nameInput.Text.Trim(),
			TimeSpan.FromMinutes((double)durationMinutes.Value),
			(double)cpuMinimum.Value,
			(double)cpuWarning.Value,
			(double)memoryPass.Value,
			(double)memoryWarning.Value,
			(double)gpuMinimum.Value,
			(double)gpuTemperature.Value,
			TimeSpan.FromSeconds((double)gpuWarmupSeconds.Value),
			TimeSpan.FromSeconds((double)gpuStabilitySeconds.Value))
		{
			Id = source.Id,
			Origin = source.Origin
		};
		QualificationProfileValidationResult validation = QualificationProfileValidator.Validate(candidate);
		if (!validation.IsValid)
		{
			MessageBox.Show(this, string.Join(Environment.NewLine, validation.Errors), "Invalid qualification profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		ResultProfile = candidate;
		DialogResult = DialogResult.OK;
		Close();
	}

	private static void AddRow(TableLayoutPanel grid, int row, string label, Control input)
	{
		grid.Controls.Add(new Label
		{
			Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft,
			Font = ThemeFonts.CardText, ForeColor = Theme.TextMain, BackColor = Theme.ApplicationBackground
		}, 0, row);
		input.Dock = DockStyle.Fill; grid.Controls.Add(input, 1, row);
	}

	private static NumericUpDown CreateNumeric(decimal minimum, decimal maximum, int decimals) => new()
	{
		Minimum = minimum, Maximum = maximum, DecimalPlaces = decimals, Increment = decimals == 0 ? 1 : 0.5m,
		BackColor = Theme.Panel, ForeColor = Theme.TextMain
	};

	private static decimal Clamp(decimal value, NumericUpDown control) =>
		Math.Min(control.Maximum, Math.Max(control.Minimum, value));
}
