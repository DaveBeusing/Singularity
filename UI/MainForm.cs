// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Core.Workloads;
using Singularity.Monitoring;
using Singularity.UI.Layout;
using Singularity.UI.Views;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private const string VersionString = "v0.1.6-alpha";

	private readonly WorkloadManager workloadManager = new();
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
			Left = LayoutConstants.HeaderLeft,
			Top = LayoutConstants.HeaderTop,
			Width = 490,
			Height = 64,
			Font = ThemeFonts.Title,
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background
		};

		Label subtitle = new()
		{
			Text = "Platform Qualification Suite",
			Left = LayoutConstants.HeaderLeft + 2,
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

		ConfigureStatusBadge();
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

		ClientSize = new Size(LayoutConstants.WindowWidth, tabHostPanel.Bottom + LayoutConstants.SectionGap);

		MinimumSize = Size;
		MaximumSize = Size;
	}

	private void ConfigureStatusBadge()
	{
		statusBadge.Left = 750;
		statusBadge.Top = 78;
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;
	}

	private void BuildTabs()
	{
		tabBarPanel.Left = LayoutConstants.MainLeft;
		tabBarPanel.Top = 140;
		tabBarPanel.Width = LayoutConstants.MainWidth;
		tabBarPanel.Height = LayoutConstants.TabBarHeight;
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
		hardwareView = new HardwareView
		{
			Left = 0,
			Top = 0
		};

		workloadsView = new WorkloadsView
		{
			Left = 0,
			Top = 0
		};

		int contentHeight = Math.Max(
			hardwareView.Height,
			workloadsView.Height);

		tabHostPanel.Left = LayoutConstants.MainLeft;
		tabHostPanel.Top = tabBarPanel.Bottom + LayoutConstants.SectionGap;
		tabHostPanel.Width = LayoutConstants.MainWidth;
		tabHostPanel.Height = contentHeight;
		tabHostPanel.AutoScroll = true;
		tabHostPanel.BackColor = Theme.Background;

		tabHostPanel.Controls.AddRange([
			hardwareView,
			workloadsView
		]);
	}

	private void ConfigureTabButton(
		Button button,
		string text,
		int left,
		ActiveTab tab)
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
		if (workloadManager.IsRunning)
			return;

		WorkloadOptions options = workloadsView.CreateOptions();

		workloadManager.Start(options);

		statusBadge.Text = "RUNNING";
		statusBadge.BackColor = Theme.Success;
		statusBadge.ForeColor = Color.White;

		SwitchTab(ActiveTab.Workloads);
	}

	private void StopWorkloads()
	{
		if (!workloadManager.IsRunning)
			return;

		workloadManager.Stop();

		statusBadge.Text = "READY";
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();

		if (workloadsView is not null)
		{
			workloadsView.UpdateMetrics(snapshot);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			timer.Dispose();
			workloadManager.Dispose();
			systemMonitor.Dispose();
		}

		base.Dispose(disposing);
	}

}