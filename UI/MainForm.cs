// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Monitoring;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private const string VersionString = "v0.1.5-alpha";

	private readonly WorkloadController workloadController = new();
	private readonly SystemMonitor systemMonitor = new();

	private readonly System.Windows.Forms.Timer timer = new();

	private readonly Button hardwareTabButton = new();
	private readonly Button workloadsTabButton = new();

	private readonly Panel tabBarPanel = new();
	private readonly Panel tabHostPanel = new();

	private readonly Label statusBadge = new();

	private HardwareView hardwareView = null!;
	private WorkloadsView workloadsView = null!;

	private enum ActiveTab
	{
		Hardware,
		Workloads
	}

	private ActiveTab activeTab = ActiveTab.Hardware;

	public MainForm()
	{
		Text = "//Singularity✦";
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		BackColor = Theme.Background;
		ForeColor = Theme.TextMain;
		Font = ThemeFonts.Title;
		AutoScaleMode = AutoScaleMode.Dpi;

		BuildUi();

		timer.Interval = 1000;
		timer.Tick += (_, _) => UpdateMonitoring();
		timer.Start();
	}

	private void BuildUi()
	{
		Controls.Clear();

		Label title = new()
		{
			Text = "//Singularity✦",
			Left = 30,
			Top = 24,
			Width = 490,
			Height = 64,
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background
		};

		Label subtitle = new()
		{
			Text = "Platform Qualification Suite",
			Left = 32,
			Top = 80,
			Width = 520,
			Height = 28,
			Font = ThemeFonts.Subtitle,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label versionLabel = new()
		{
			Text = VersionString,
			Left = 750,
			Top = 32,
			Width = 130,
			Height = 24,
			Font = ThemeFonts.SectionHeader,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleRight
		};

		statusBadge.Left = 750;
		statusBadge.Top = 78;
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;

		BuildTabs();
		BuildViews();

		Controls.AddRange([
			title,
			subtitle,
			versionLabel,
			statusBadge,
			tabBarPanel,
			tabHostPanel
		]);

		workloadsView.StartButton.Click += (_, _) => StartWorkloads();
		workloadsView.StopButton.Click += (_, _) => StopWorkloads();

		SwitchTab(ActiveTab.Hardware);

		Size windowSize = new(920, tabHostPanel.Bottom + 40);
		ClientSize = windowSize;
		MinimumSize = windowSize;
	}

	private void BuildTabs()
	{
		tabBarPanel.Left = 40;
		tabBarPanel.Top = 140;
		tabBarPanel.Width = 840;
		tabBarPanel.Height = 54;
		tabBarPanel.BackColor = Theme.Panel;

		ConfigureTabButton(hardwareTabButton, "PLATFORM", 20, ActiveTab.Hardware);
		ConfigureTabButton(workloadsTabButton, "WORKLOADS", 250, ActiveTab.Workloads);

		tabBarPanel.Controls.AddRange([
			hardwareTabButton,
			workloadsTabButton
		]);
	}

	private void BuildViews()
	{
		tabHostPanel.Left = 40;
		tabHostPanel.Top = tabBarPanel.Bottom + 20;
		tabHostPanel.Width = 840;
		tabHostPanel.BackColor = Theme.Background;

		hardwareView = new HardwareView();
		workloadsView = new WorkloadsView();

		tabHostPanel.Height = Math.Max(hardwareView.Height, workloadsView.Height);

		hardwareView.Left = 0;
		hardwareView.Top = 0;

		workloadsView.Left = 0;
		workloadsView.Top = 0;

		tabHostPanel.Controls.AddRange([
			hardwareView,
			workloadsView
		]);
	}

	private void ConfigureTabButton(Button button, string text, int left, ActiveTab tab)
	{
		button.Text = text;
		button.Left = left;
		button.Top = 10;
		button.Width = tab == ActiveTab.Hardware ? 220 : 240;
		button.Height = 34;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = ThemeFonts.Button;
		button.Click += (_, _) => SwitchTab(tab);
	}

	private void SwitchTab(ActiveTab tab)
	{
		activeTab = tab;

		hardwareView.Visible = activeTab == ActiveTab.Hardware;
		workloadsView.Visible = activeTab == ActiveTab.Workloads;

		hardwareTabButton.BackColor = activeTab == ActiveTab.Hardware ? Theme.Accent : Theme.PanelLight;
		hardwareTabButton.ForeColor = activeTab == ActiveTab.Hardware ? Color.Black : Theme.TextMain;

		workloadsTabButton.BackColor = activeTab == ActiveTab.Workloads ? Theme.Accent : Theme.PanelLight;
		workloadsTabButton.ForeColor = activeTab == ActiveTab.Workloads ? Color.Black : Theme.TextMain;
	}

	private void StartWorkloads()
	{
		if (workloadController.IsRunning)
			return;

		WorkloadOptions options = workloadsView.CreateOptions();

		workloadController.Configure(options);
		workloadController.Start();

		statusBadge.Text = "RUNNING";
		statusBadge.BackColor = Theme.Success;
		statusBadge.ForeColor = Color.White;
	}

	private void StopWorkloads()
	{
		if (!workloadController.IsRunning)
			return;

		workloadController.Stop();

		statusBadge.Text = "READY";
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();
		workloadsView.UpdateMetrics(snapshot);
	}

}