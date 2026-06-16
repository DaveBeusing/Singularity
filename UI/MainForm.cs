// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Monitoring;
using Singularity.Hardware.Providers;
using Singularity.Hardware.Models;
using Singularity.UI;
using Singularity.UI.Controls;
using Singularity.UI.Panels;
using Singularity.UI.Cards;

namespace Singularity.UI;

public sealed class MainForm : Form
{
	private const string VersionString = "v0.1.5-alpha";

	private readonly WorkloadController workloadController = new();
	private readonly SystemMonitor systemMonitor = new();
	private readonly SingularityCheckBox cpuCheck = new();
	private readonly SingularityCheckBox memoryCheck = new();
	private readonly SingularityCheckBox gpuCheck = new();

	private readonly SingularityNumeric cpuThreadsInput = new();
	private readonly SingularityNumeric memoryGbInput = new();
	private readonly SingularityNumeric gpuLoadInput = new();

	private readonly Button startButton = new();
	private readonly Button stopButton = new();
	private readonly Label countdownLabel = new();

	private readonly Label statusBadge = new();

	private MetricsPanel cpuMetricCard = null!;
	private MetricsPanel appRamMetricCard = null!;
	private MetricsPanel systemRamMetricCard = null!;


	private readonly System.Windows.Forms.Timer timer = new();

	private readonly HardwareProvider hardwareProvider = new();

	private readonly Label boardInfoValue = new();
	private readonly Label cpuInfoValue = new();
	private readonly Label gpuInfoValue = new();


	private const int MainLeft = 40;
	private const int MainWidth = 840;
	private const int SectionGap = 20;
	private const int CardHeight = 78;
	private const int HardwareCardHeight = 78;
	private const int HardwareCardWidth = 800;
	private const int HardwareCardLeft = 20;
	private const int HardwareCardGap = 10;
	private const int StandardHardwareCardHeight = 78;
	private const int LargeHardwareCardHeight = 110;//92
	private const int SectionHeaderHeight = 55;


	//tabs
	private readonly Button hardwareTabButton = new();
	private readonly Button workloadsTabButton = new();
	private readonly Panel tabBarPanel = new();
	private readonly Panel tabHostPanel = new();
	private readonly Panel hardwareTabPanel = new();
	private readonly Panel workloadsTabPanel = new();
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
			Top = 32,//78
			Width = 130,
			Height = 24,
			Font = ThemeFonts.SectionHeader,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleRight
		};

		//Status Badge
		statusBadge.Left = 750;
		statusBadge.Top = 78;//32
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;


		//tabs - ospanel bleibt noch stehen, mal gucken wie wir das später besser integrieren können
		//Panel tabBarPanel = CreatePanel(40, osPanel.Bottom + 20, 840, 54);
		//tab bar
		tabBarPanel.Left = 40;
		tabBarPanel.Top = 140;
		tabBarPanel.Width = 840;
		tabBarPanel.Height = 54;
		tabBarPanel.BackColor = Theme.Panel;

		hardwareTabButton.Text = "PLATFORM";
		hardwareTabButton.Left = 20;
		hardwareTabButton.Top = 10;
		hardwareTabButton.Width = 220;
		hardwareTabButton.Height = 34;
		hardwareTabButton.Click += (_, _) => SwitchTab(ActiveTab.Hardware);

		workloadsTabButton.Text = "WORKLOADS";
		workloadsTabButton.Left = 250;
		workloadsTabButton.Top = 10;
		workloadsTabButton.Width = 240;
		workloadsTabButton.Height = 34;
		workloadsTabButton.Click += (_, _) => SwitchTab(ActiveTab.Workloads);

		tabBarPanel.Controls.Add(hardwareTabButton);
		tabBarPanel.Controls.Add(workloadsTabButton);

		//tab panels
		tabHostPanel.Left = 40;
		tabHostPanel.Top = tabBarPanel.Bottom + 20;
		tabHostPanel.Width = 840;
		tabHostPanel.BackColor = Theme.Background;

		hardwareTabPanel.Left = 0;
		hardwareTabPanel.Top = 0;
		hardwareTabPanel.Width = tabHostPanel.Width;
		hardwareTabPanel.Height = tabHostPanel.Height;
		hardwareTabPanel.BackColor = Theme.Background;

		workloadsTabPanel.Left = 0;
		workloadsTabPanel.Top = 0;
		workloadsTabPanel.Width = tabHostPanel.Width;
		workloadsTabPanel.Height = tabHostPanel.Height;
		workloadsTabPanel.BackColor = Theme.Background;


		//
		// HARDWRAE TAB
		//
		
		//OsInfo osInfo = osInfoReader.Read();
		//HardwareInfo hardware = hardwareInfoReader.Read();
		HardwareInventory inventory = hardwareProvider.Read();
		OsInventory osInventory = inventory.Os;

		int hardwareContentTop = 0;

		Panel osPanel = CreatePanel(0, hardwareContentTop, MainWidth, 150);
		AddSectionHeader(osPanel, SingularityIconType.Metrics, "OS");
		OsInfoPanel osInfoPanel = new(osInventory, 800, 78)
		{
			Left = 20,
			Top = 55
		};
		osPanel.Controls.Add(osInfoPanel);
		//hardwareTabPanel.Controls.Add(osPanel);

		hardwareContentTop = osPanel.Bottom + 20;

		//Hardware Panel
		int memoryCount = inventory.MemoryModules.Count;

		int hardwarePanelHeight =
			SectionHeaderHeight +
			StandardHardwareCardHeight + HardwareCardGap +					// Board
			StandardHardwareCardHeight + HardwareCardGap +					// CPU
			LargeHardwareCardHeight + HardwareCardGap +						// GPU
			memoryCount * (LargeHardwareCardHeight + HardwareCardGap) +
			10;

		Panel hardwarePanel = CreatePanel(0, hardwareContentTop, MainWidth, hardwarePanelHeight);
		AddSectionHeader(hardwarePanel, SingularityIconType.Motherboard, "HARDWARE");

		int hardwareTop = SectionHeaderHeight;
		MainboardInfoPanel mainboardInfoPanel = new(inventory.Mainboard, HardwareCardWidth, StandardHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};
		hardwareTop += StandardHardwareCardHeight + HardwareCardGap;

		CpuInfoPanel cpuInfoPanel = new(inventory.Cpu, HardwareCardWidth, LargeHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};
		hardwareTop += LargeHardwareCardHeight + HardwareCardGap;



		GpuInfoPanel gpuInfoPanel = new(inventory.Gpu, HardwareCardWidth, LargeHardwareCardHeight)
		{
			Left = HardwareCardLeft,
			Top = hardwareTop
		};
		hardwareTop += LargeHardwareCardHeight + HardwareCardGap;

		hardwarePanel.Controls.AddRange([
			mainboardInfoPanel,
			cpuInfoPanel,
			gpuInfoPanel
		]);

		foreach (MemoryInventory module in inventory.MemoryModules)
		{
			MemoryInfoPanel memoryInfoPanel = new(module, HardwareCardWidth, LargeHardwareCardHeight)
			{
				Left = HardwareCardLeft,
				Top = hardwareTop
			};
			hardwarePanel.Controls.Add(memoryInfoPanel);
			hardwareTop += LargeHardwareCardHeight + HardwareCardGap;
		}

		hardwarePanel.Height = hardwareTop + 10;
		//dynamische höhe je nach RAM count für darunter liegende panels
		//int lowerPanelsTop = hardwarePanel.Top + hardwarePanel.Height + 20;


		//
		//WORKLOADS TAB
		//


		//Workloads Panel
		//Panel workloadsPanel = CreatePanel(MainLeft, lowerPanelsTop, 405, 365);
		//tabs -> lebt jetzt in hardwareTabPanel.Controls 
		Panel workloadsPanel = CreatePanel(0, 0, 405, 365);
		AddSectionHeader(workloadsPanel, SingularityIconType.Cpu, "WORKLOADS");

		//CPU Workload
		Panel cpuCard = CreateCard(20, 60, 365, CardHeight);
		ConfigureCheckBox(cpuCheck, 22, 25, true);
		SingularityIcon cpuIcon = CreateIcon(SingularityIconType.Cpu, 60, 24, Theme.TextMuted);
		Label cpuNameLabel = CreateValueLabel("CPU", 105, 23, 75);
		Label cpuThreadsLabel = CreateMutedLabel("Threads", 190, 27, 75);
		ConfigureNumeric(cpuThreadsInput, 270, 16, Environment.ProcessorCount, 1, Environment.ProcessorCount * 4);
		cpuCard.Controls.AddRange([
			cpuCheck,
			cpuIcon,
			cpuNameLabel,
			cpuThreadsLabel,
			cpuThreadsInput
		]);

		//RAM Workload
		Panel ramCard = CreateCard(20, 150, 365, CardHeight);
		ConfigureCheckBox(memoryCheck, 22, 25, true);
		SingularityIcon memoryIcon = CreateIcon(SingularityIconType.Memory, 60, 24, Theme.TextMuted);
		Label ramNameLabel = CreateValueLabel("RAM", 105, 23, 75);
		Label ramGbLabel = CreateMutedLabel("GB", 190, 27, 75);
		ConfigureNumeric(memoryGbInput, 270, 16, 8, 1, 1024);
		ramCard.Controls.AddRange([
			memoryCheck,
			memoryIcon,
			ramNameLabel,
			ramGbLabel,
			memoryGbInput
		]);

		//GPU Workload
		Panel gpuCard = CreateCard(20, 240, 365, CardHeight);
		ConfigureCheckBox(gpuCheck, 22, 25, false);
		SingularityIcon gpuIcon = CreateIcon(SingularityIconType.Gpu, 60, 24, Theme.TextMuted);
		Label gpuNameLabel = CreateValueLabel("GPU", 105, 23, 75);
		Label gpuLoadLabel = CreateMutedLabel("Load %", 190, 27, 75);
		ConfigureNumeric(gpuLoadInput, 270, 16, 100, 1, 100);
		gpuCard.Controls.AddRange([
			gpuCheck,
			gpuIcon,
			gpuNameLabel,
			gpuLoadLabel,
			gpuLoadInput
		]);

		//Fill workloadsPanel
		workloadsPanel.Controls.AddRange([
			cpuCard,
			ramCard,
			gpuCard
		]);

		// Metrics Panel
		//
		Panel metricsPanel = CreatePanel(435, 0, 405, 365);
		AddSectionHeader(metricsPanel, SingularityIconType.Metrics, "LIVE METRICS");

		cpuMetricCard = new MetricsPanel("APP CPU", Theme.Success, 360, CardHeight)
		{
			Left = 20,
			Top = 60
		};
		appRamMetricCard = new MetricsPanel("APP RAM", Theme.Accent, 360, CardHeight)
		{
			Left = 20,
			Top = 150
		};
		systemRamMetricCard = new MetricsPanel("SYSTEM RAM", Theme.Danger, 360, CardHeight)
		{
			Left = 20,
			Top = 240
		};

		metricsPanel.Controls.AddRange([
			cpuMetricCard,
			appRamMetricCard,
			systemRamMetricCard
		]);





		//
		//Control Panel
		//int controlPanelTop = lowerPanelsTop + 365 + SectionGap;
		//Panel controlPanel = CreatePanel(MainLeft, controlPanelTop, MainWidth, 80);
		Panel controlPanel = CreatePanel(0, metricsPanel.Bottom + 20, MainWidth, 80);

		startButton.Text = "START TEST";
		startButton.Left = 20;
		startButton.Top = 18;
		startButton.Width = 230;
		startButton.Height = 44;
		StyleButton(startButton, Theme.Success, Color.Black);
		startButton.Click += (_, _) => StartWorkload();

		stopButton.Text = "STOP TEST";
		stopButton.Left = 270;
		stopButton.Top = 18;
		stopButton.Width = 230;
		stopButton.Height = 44;
		StyleButton(stopButton, Theme.Danger, Color.Black);
		stopButton.Enabled = false;
		stopButton.Click += (_, _) => StopWorkload();

		countdownLabel.Text = "00:00:00 / 00:00:00";
		countdownLabel.Left = 540;
		countdownLabel.Top = 22;
		countdownLabel.Width = 280;
		countdownLabel.Height = 36;
		countdownLabel.Font = new Font("Consolas", 13F, FontStyle.Bold);
		countdownLabel.ForeColor = Theme.TextMuted;
		countdownLabel.BackColor = Theme.Panel;
		countdownLabel.TextAlign = ContentAlignment.MiddleRight;

		//Fill controlPanel
		controlPanel.Controls.AddRange([
			startButton,
			stopButton,
			countdownLabel
		]);



		//tabhost dynamic height
		int hardwareTabHeight = Math.Max(osPanel.Bottom, hardwarePanel.Bottom) + 20;
		int workloadsTabHeight = controlPanel.Bottom + 20;//365
		int tabHostHeight = Math.Max(hardwareTabHeight, workloadsTabHeight);
		tabHostPanel.Height = tabHostHeight;
		hardwareTabPanel.Height = tabHostHeight;
		workloadsTabPanel.Height = tabHostHeight;

		tabHostPanel.Controls.AddRange([
			hardwareTabPanel,
			workloadsTabPanel
		]);

		//tabHostPanel.Controls.Add(hardwareTabPanel);
		//tabHostPanel.Controls.Add(workloadsTabPanel);


		//Tabs
		hardwareTabPanel.Controls.AddRange([
			osPanel,
			hardwarePanel
		]);
		workloadsTabPanel.Controls.AddRange([
			workloadsPanel,
			metricsPanel,
			controlPanel
		]);

		//Root controls
		Controls.AddRange([
			title,
			subtitle,
			versionLabel,
			statusBadge,
			tabBarPanel,
			tabHostPanel
		]);

		SwitchTab(ActiveTab.Hardware);

		//Dynamic window sizing
		//Size windowSize = new(920, controlPanelTop + 115);
		//tabs
		Size windowSize = new(920, tabHostPanel.Bottom + 40);
		ClientSize = windowSize;
		MinimumSize = windowSize;

	}

	private static Panel CreatePanel(int left, int top, int width, int height)
	{
		return new Panel
		{
			Left = left,
			Top = top,
			Width = width,
			Height = height,
			BackColor = Theme.Panel
		};
	}

	private static Panel CreateCard(int left, int top, int width, int height)
	{
		return new Panel
		{
			Left = left,
			Top = top,
			Width = width,
			Height = height,
			BackColor = Theme.PanelLight
		};
	}

	private static Label CreateSectionTitle(string text, int left, int top)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = 250,
			Height = 25,
			Font = new Font("Segoe UI", 10, FontStyle.Bold),
			ForeColor = Theme.Accent,
			BackColor = Theme.Panel
		};
	}

	private static void AddSectionHeader(Panel parent, SingularityIconType iconType, string title)
	{
		SingularityIcon icon = new()
		{
			IconType = iconType,
			IconColor = Theme.Accent,
			Left = 20,
			Top = 14,
			Width = 32,
			Height = 32,
			BackColor = Theme.Panel
		};
		Label label = CreateSectionTitle(title, 60, 14);
		parent.Controls.AddRange([
			icon,
			label
		]);
	}

	private static Label CreateMutedLabel(string text, int left, int top, int width)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 22,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};
	}

	private static void ConfigureCheckBox(SingularityCheckBox  checkBox, int left, int top, bool isChecked)
	{
		checkBox.Left = left;
		checkBox.Top = top;
		checkBox.Width = 28;
		checkBox.Height = 28;
		checkBox.Checked = isChecked;
		checkBox.BackColor = Theme.PanelLight;
	}

	private static void ConfigureNumeric(SingularityNumeric  numeric, int left, int top, int value, int minimum, int maximum)
	{
		numeric.Left = left;
		numeric.Top = top;
		numeric.Width = 82;
		numeric.Height = 42;
		numeric.Minimum = minimum;
		numeric.Maximum = maximum;
		numeric.Value = value;
		numeric.BackColor = Theme.Background;
	}

	private static void StyleButton(Button button, Color background, Color foreground)
	{
		button.BackColor = background;
		button.ForeColor = foreground;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = ThemeFonts.Button;
		button.Cursor = Cursors.Hand;
	}


	private static SingularityIcon CreateIcon(SingularityIconType type, int left, int top, Color color)
	{
		return new SingularityIcon
		{
			IconType = type,
			IconColor = color,
			Left = left,
			Top = top,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};
	}

	private static Label CreateValueLabel(string text, int left, int top, int width)
	{
		return new Label
		{
			Text = text,
			Left = left,
			Top = top,
			Width = width,
			Height = 28,
			Font = new Font("Segoe UI", 11F, FontStyle.Regular),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private void StartWorkload()
	{
		WorkloadOptions options = new()
		{
			EnableCpuWorkload = cpuCheck.Checked,
			EnableMemoryWorkload = memoryCheck.Checked,
			EnableGpuWorkload = gpuCheck.Checked,
			CpuThreads = (int)cpuThreadsInput.Value,
			MemoryGb = (int)memoryGbInput.Value,
			GpuLoadPercent = (int)gpuLoadInput.Value
		};
		workloadController.Configure(options);
		workloadController.Start();
		SetInputEnabled(false);
		startButton.Enabled = false;
		stopButton.Enabled = true;
		statusBadge.Text = "RUNNING";
		statusBadge.BackColor = Theme.Success;
		statusBadge.ForeColor = Color.Black;
	}

	private void StopWorkload()
	{
		workloadController.Stop();
		SetInputEnabled(true);
		startButton.Enabled = true;
		stopButton.Enabled = false;
		statusBadge.Text = "STOPPED";
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;
	}

	private void SetInputEnabled(bool enabled)
	{
		cpuCheck.Enabled = enabled;
		memoryCheck.Enabled = enabled;
		gpuCheck.Enabled = enabled;
		cpuThreadsInput.Enabled = enabled;
		memoryGbInput.Enabled = enabled;
		gpuLoadInput.Enabled = enabled;
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();
		double appRamPercent = snapshot.ProcessMemoryMb / 32000.0 * 100.0;

		cpuMetricCard.ValueLabel.Text = $"{snapshot.ProcessCpuPercent:F1} %";
		appRamMetricCard.ValueLabel.Text = $"{snapshot.ProcessMemoryMb:N0} MB";
		systemRamMetricCard.ValueLabel.Text = $"{snapshot.UsedPhysicalMemoryPercent:F1} % | {snapshot.UsedPhysicalMemoryMb:N0} / {snapshot.TotalPhysicalMemoryMb:N0} MB";

		cpuMetricCard.Bar.Value = (int)Math.Clamp(snapshot.ProcessCpuPercent, 0, 100);
		appRamMetricCard.Bar.Value = (int)Math.Clamp(appRamPercent, 0, 100);
		systemRamMetricCard.Bar.Value = (int)Math.Clamp(snapshot.UsedPhysicalMemoryPercent, 0, 100);

	}

	private void SwitchTab(ActiveTab tab)
	{
		activeTab = tab;
		hardwareTabPanel.Visible = activeTab == ActiveTab.Hardware;
		workloadsTabPanel.Visible = activeTab == ActiveTab.Workloads;
		if (activeTab == ActiveTab.Hardware)
		{
			hardwareTabPanel.BringToFront();
		}
		else
		{
			workloadsTabPanel.BringToFront();
		}
		StyleTabButton(hardwareTabButton, activeTab == ActiveTab.Hardware);
		StyleTabButton(workloadsTabButton, activeTab == ActiveTab.Workloads);
	}

	private static void StyleTabButton(Button button, bool isActive)
	{
		button.BackColor = isActive ? Theme.Accent : Theme.PanelLight;
		button.ForeColor = isActive ? Color.Black : Theme.TextMain;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		button.Cursor = Cursors.Hand;
	}


	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		workloadController.Stop();
		base.OnFormClosing(e);
	}
}