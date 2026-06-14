// Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.

using Singularity.Core;
using Singularity.Monitoring;
using Singularity.UI.Controls;

namespace Singularity.UI;

/// <summary>
/// Hauptfenster der Anwendung.
/// Diese Version nutzt ein dunkles Dashboard-Layout
/// mit klar getrennten Bereichen für Workloads und Live-Metriken.
/// </summary>
public sealed class MainForm : Form
{
	private const string VersionString = "v0.1.3-alpha";

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
	private readonly Label cpuMetricValue = new();
	private readonly Label appRamMetricValue = new();
	private readonly Label systemRamMetricValue = new();

	private readonly System.Windows.Forms.Timer timer = new();

	private readonly MetricBar cpuBar = new();
	private readonly MetricBar appRamBar = new();
	private readonly MetricBar systemRamBar = new();

	private readonly HardwareInfoReader hardwareInfoReader = new();
	private readonly OsInfoReader osInfoReader = new();

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



	public MainForm()
	{
		Text = "//Singularity✦";
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		BackColor = Theme.Background;
		ForeColor = Theme.TextMain;
		Font = new Font("Segoe UI", 9F);
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
			Font = new Font("Cascadia Mono", 24F, FontStyle.Bold),
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
			Font = new Font("Segoe UI", 10F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleLeft
		};

		Label versionLabel = new()
		{
			Text = VersionString,
			Left = 740,
			Top = 32,//78
			Width = 130,
			Height = 24,
			Font = new Font("Consolas", 9F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleRight
		};

		//Status Badge
		statusBadge.Left = 740;
		statusBadge.Top = 78;//32
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;

		//OS panel
		OsInfo osInfo = osInfoReader.Read();
		Panel osPanel = CreatePanel(MainLeft, 150, MainWidth, 150);
		AddSectionHeader(osPanel, SingularityIconType.Metrics, "SYSTEM OS");
		Panel osCard = CreateOsInfoCard(osInfo, 20, 55, 800);
		osPanel.Controls.Add(osCard);


		//Hardware Panel
		HardwareInfo hardware = hardwareInfoReader.Read();
		int hardwarePanelTop = osPanel.Top + osPanel.Height + 20;
		int hardwareCardCount = 3 + hardware.MemoryModules.Count;
		int memoryCount = hardware.MemoryModules.Count;

		int hardwarePanelHeight =
			SectionHeaderHeight +
			StandardHardwareCardHeight + HardwareCardGap +					// Board
			StandardHardwareCardHeight + HardwareCardGap +					// CPU
			LargeHardwareCardHeight + HardwareCardGap +						// GPU
			memoryCount * (LargeHardwareCardHeight + HardwareCardGap) +
			10;

		Panel hardwarePanel = CreatePanel(MainLeft, hardwarePanelTop, MainWidth, hardwarePanelHeight);

		AddSectionHeader(hardwarePanel, SingularityIconType.Motherboard, "SYSTEM HARDWARE");

		int hardwareTop = SectionHeaderHeight;
		Panel boardCardInfo = CreateHardwareInfoCard(SingularityIconType.Motherboard, "BOARD", hardware.Mainboard, hardware.MainboardDetails, HardwareCardLeft, hardwareTop, HardwareCardWidth);

		//hardwareTop += StandardHardwareCardHeight + HardwareCardGap;
		//Panel cpuCardInfo = CreateHardwareInfoCard(SingularityIconType.Cpu, "CPU", hardware.Cpu, hardware.CpuDetails, HardwareCardLeft, hardwareTop, HardwareCardWidth);
		hardwareTop += 92 + HardwareCardGap;
		Panel cpuCardInfo = CreateCpuInfoCard(hardware, HardwareCardLeft, hardwareTop, HardwareCardWidth);

		hardwareTop += LargeHardwareCardHeight + HardwareCardGap;
		Panel gpuCardInfo = CreateGpuInfoCard( hardware, 20, hardwareTop, HardwareCardWidth);

		hardwareTop += LargeHardwareCardHeight  + HardwareCardGap;

		hardwarePanel.Controls.AddRange([
			boardCardInfo,
			cpuCardInfo,
			gpuCardInfo
		]);

		foreach (MemoryModuleInfo module in hardware.MemoryModules)
		{
			Panel memoryCard = CreateMemoryInfoCard(module, HardwareCardLeft, hardwareTop, HardwareCardWidth);
			hardwarePanel.Controls.Add(memoryCard);
			hardwareTop += LargeHardwareCardHeight  + HardwareCardGap;
		}

		//dynamische höhe je nach RAM count für darunter liegende panels
		int lowerPanelsTop = hardwarePanel.Top + hardwarePanel.Height + 20;

		//Workloads Panel
		Panel workloadsPanel = CreatePanel(MainLeft, lowerPanelsTop, 405, 365);
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

		//Metrics Panel
		cpuBar.FillColor = Theme.Success;
		appRamBar.FillColor = Theme.Accent;
		systemRamBar.FillColor = Theme.Danger;

		Panel metricsPanel = CreatePanel(475, lowerPanelsTop, 405, 365);
		AddSectionHeader(metricsPanel, SingularityIconType.Metrics, "LIVE METRICS");
		Panel cpuMetric = CreateMetricCard("APP CPU", cpuMetricValue, cpuBar, 20, 60);
		Panel appRamMetric = CreateMetricCard("APP RAM", appRamMetricValue, appRamBar, 20, 150);
		Panel systemRamMetric = CreateMetricCard("SYSTEM RAM", systemRamMetricValue, systemRamBar, 20, 240);
		metricsPanel.Controls.AddRange([
			cpuMetric,
			appRamMetric,
			systemRamMetric
		]);

		//Control Panel
		int controlPanelTop = lowerPanelsTop + 365 + SectionGap;
		Panel controlPanel = CreatePanel(MainLeft, controlPanelTop, MainWidth, 80);

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

		//Fill 
		Controls.AddRange([
			title,
			subtitle,
			versionLabel,
			statusBadge,
			osPanel,
			hardwarePanel,
			workloadsPanel,
			metricsPanel,
			controlPanel
		]);

		//Dynamic window sizing
		Size windowSize = new(920, controlPanelTop + 115);
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

	private static Panel CreateMetricCard(string title, Label valueLabel, MetricBar bar, int left, int top)
	{
		Panel card = CreateCard(left, top, 360, CardHeight);
		Label titleLabel = new()
		{
			Text = title,
			Left = 18,
			Top = 12,
			Width = 300,
			Height = 20,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		valueLabel.Left = 18;
		valueLabel.Top = 32;
		valueLabel.Width = 315;
		valueLabel.Height = 30;
		valueLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
		valueLabel.ForeColor = Theme.TextMain;
		valueLabel.BackColor = Theme.PanelLight;
		valueLabel.Text = "-";
		valueLabel.AutoEllipsis = true;

		bar.Left = 18;
		bar.Top = 64;
		bar.Width = 329;
		bar.Height = 7;
		bar.BackColor = Theme.PanelLight;

		card.Controls.AddRange([
			titleLabel,
			valueLabel,
			bar
		]);
		return card;
	}

	private static Panel CreateOsInfoCard(OsInfo osInfo, int left, int top, int width)
	{
		Panel card = CreateCard(left, top, width, 78);
		Label line1 = new()
		{
			Text = osInfo.Name,
			Left = 18,
			Top = 10,
			Width = width - 36,
			Height = 20,
			Font = new Font("Consolas", 9.5F),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label line2 = new()
		{
			Text = $"Version {osInfo.Version} | {osInfo.Architecture} | Build {osInfo.BuildNumber}",
			Left = 18,
			Top = 32,
			Width = width - 36,
			Height = 18,
			Font = new Font("Consolas", 8.5F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label line3 = new()
		{
			Text = $"Installed {osInfo.InstallDate} | Boot {osInfo.LastBootTime}",//{osInfo.User}
			Left = 18,
			Top = 52,
			Width = width - 36,
			Height = 18,
			Font = new Font("Consolas", 8.5F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		card.Controls.AddRange([
			line1,
			line2,
			line3
		]);
		return card;
	}



	private static Panel CreateHardwareInfoCard(SingularityIconType iconType, string title, string line1, string line2, int left, int top, int width)
	{
		Panel card = CreateCard(left, top, width, StandardHardwareCardHeight);
		SingularityIcon icon = new()
		{
			IconType = iconType,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 23,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};
		Label titleLabel = new()
		{
			Text = title,
			Left = 64,
			Top = 10,
			Width = width - 84,
			Height = 20,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};
		Label valueLabel = new()
		{
			Text = line1,
			Left = 64,
			Top = 30,
			Width = width - 84,
			Height = 20,
			Font = new Font("Consolas", 8.5F, FontStyle.Regular),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label detailLabel = new()
		{
			Text = line2,
			Left = 64,
			Top = 50,
			Width = width - 84,
			Height = 20,
			Font = new Font("Consolas", 8.5F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		card.Controls.AddRange([
			icon,
			titleLabel,
			valueLabel,
			detailLabel
		]);
		return card;
	}

	private static Panel CreateCpuInfoCard(HardwareInfo hardware, int left, int top, int width)
	{
		const int height = LargeHardwareCardHeight;
		Panel card = CreateCard(left, top, width, height);
		SingularityIcon icon = new()
		{
			IconType = SingularityIconType.Cpu,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};

		Label titleLabel = new()
		{
			Text = "CPU",
			Left = 64,
			Top = 8,
			Width = 200,
			Height = 18,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label nameLabel = new()
		{
			Text = hardware.Cpu,
			Left = 64,
			Top = 28,
			Width = width - 84,
			Height = 18,
			Font = new Font("Consolas", 8.8F),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		string[] parts = hardware.CpuDetails.Split('|');

		Label line2 = new()
		{
			Text = parts.Length > 0 ? parts[0].Trim() : "",
			Left = 64,
			Top = 48,
			Width = width - 84,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label line3 = new()
		{
			Text = parts.Length > 1 ? parts[1].Trim() : "",
			Left = 64,
			Top = 66,
			Width = width - 84,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label line4 = new()
		{
			Text = parts.Length > 2 ? string.Join(" | ", parts.Skip(2)).Trim() : "",
			Left = 64,
			Top = 84,
			Width = width - 84,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		card.Controls.AddRange([
			icon,
			titleLabel,
			nameLabel,
			line2,
			line3,
			line4
		]);
		return card;
	}

	private static Panel CreateGpuInfoCard(HardwareInfo hardware, int left, int top, int width)
	{
		const int height = LargeHardwareCardHeight;
		Panel card = CreateCard(left, top, width, height);
		SingularityIcon icon = new()
		{
			IconType = SingularityIconType.Gpu,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};

		Label titleLabel = new()
		{
			Text = "GPU",
			Left = 64,
			Top = 8,
			Width = 250,
			Height = 20,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label nameLabel = new()
		{
			Text = hardware.Gpu,
			Left = 64,
			Top = 29,
			Width = width - 260,
			Height = 20,
			Font = new Font("Consolas", 8.8F),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label pcieCurrentLabel = new()
		{
			Text = $"Link ⇄ PCIe {hardware.GpuPcieCurrent}",
			Left = 64,
			Top = 50,
			Width = width - 260,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label pcieMaxLabel = new()
		{
			Text = $"Host ⇄ PCIe {hardware.GpuPcieMax}",
			Left = 64,
			Top = 68,
			Width = width - 260,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label vramLabel = new()
		{
			Text = hardware.GpuVram,
			Left = width - 185,
			Top = 29,
			Width = 165,
			Height = 20,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleRight
		};

		Label tempLabel = new()
		{
			Text = hardware.GpuTemperature,
			Left = width - 185,
			Top = 50,
			Width = 165,
			Height = 20,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleRight
		};

		card.Controls.AddRange([
			icon,
			titleLabel,
			nameLabel,
			pcieCurrentLabel,
			pcieMaxLabel,
			vramLabel,
			tempLabel
		]);
		return card;
	}


	private static Panel CreateMemoryInfoCard(MemoryModuleInfo module,int left,int top,int width)
	{
		const int height = LargeHardwareCardHeight;
		Panel card = CreateCard(left, top, width, height);
		SingularityIcon icon = new()
		{
			IconType = SingularityIconType.Memory,
			IconColor = Theme.Accent,
			Left = 14,
			Top = 29,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};

		Label titleLabel = new()
		{
			Text = "MEMORY",
			Left = 64,
			Top = 8,
			Width = 200,
			Height = 20,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label slotLabel = new()
		{
			Text = module.Slot,
			Left = 64,
			Top = 28,
			Width = width - 280,
			Height = 18,
			Font = new Font("Consolas", 8.8F),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label specLabel = new()
		{
			Text =
				$"{module.Capacity} " +
				$"{module.MemoryType} " +
				$"{module.DimmType} " +
				$"{module.EccType}",
			Left = 64,
			Top = 48,
			Width = width - 280,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		Label speedLabel = new()
		{
			Text = module.Speed,
			Left = 64,
			Top = 66,
			Width = width - 280,
			Height = 16,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};

		Label manufacturerLabel = new()
		{
			Text = module.Manufacturer,
			Left = width - 220,
			Top = 28,
			Width = 200,
			Height = 18,
			Font = new Font("Consolas", 8.8F, FontStyle.Bold),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleRight
		};

		Label partNumberLabel = new()
		{
			Text = module.PartNumber,
			Left = width - 220,
			Top = 48,
			Width = 200,
			Height = 18,
			Font = new Font("Consolas", 8.2F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight,
			TextAlign = ContentAlignment.MiddleRight,
			AutoEllipsis = true
		};

		card.Controls.AddRange([
			icon,
			titleLabel,
			slotLabel,
			specLabel,
			speedLabel,
			manufacturerLabel,
			partNumberLabel
		]);
		return card;
	}







	private static void StyleButton(Button button, Color background, Color foreground)
	{
		button.BackColor = background;
		button.ForeColor = foreground;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
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
		cpuMetricValue.Text = $"{snapshot.ProcessCpuPercent:F1} %";
		appRamMetricValue.Text = $"{snapshot.ProcessMemoryMb:N0} MB";
		systemRamMetricValue.Text =	$"{snapshot.UsedPhysicalMemoryPercent:F1} % | {snapshot.UsedPhysicalMemoryMb:N0} / {snapshot.TotalPhysicalMemoryMb:N0} MB";
		cpuBar.Value = (int)Math.Clamp(snapshot.ProcessCpuPercent, 0, 100);
		double appRamPercent = snapshot.ProcessMemoryMb / 32000.0 * 100.0;
		appRamBar.Value = (int)Math.Clamp(appRamPercent, 0, 100);
		systemRamBar.Value = (int)Math.Clamp(snapshot.UsedPhysicalMemoryPercent, 0, 100);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		workloadController.Stop();
		base.OnFormClosing(e);
	}
}