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
	private readonly Label boardInfoValue = new();
	private readonly Label cpuInfoValue = new();
	private readonly Label gpuInfoValue = new();

	private const int CardHeight = 78;

	public MainForm()
	{
		Text = "Singularity";
		ClientSize = new Size(900, 920);
		MinimumSize = new Size(900, 920);
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
			Text = "//SINGULARITY◉",
			Left = 30,
			Top = 24,
			Width = 490,
			Height = 56,
			Font = new Font("Cascadia Mono", 24F, FontStyle.Bold),
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background
		};

		Label subtitle = new()
		{
			Text = "Platform Qualification Suite",
			Left = 32,
			Top = 78,
			Width = 520,
			Height = 28,
			Font = new Font("Segoe UI", 10F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background,
			TextAlign = ContentAlignment.MiddleLeft
		};

		//Status Badge
		statusBadge.Left = 740;
		statusBadge.Top = 32;
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;

		cpuBar.FillColor = Theme.Success;
		appRamBar.FillColor = Theme.Accent;
		systemRamBar.FillColor = Theme.Danger;

		//Hardware Panel
		HardwareInfo hardware = hardwareInfoReader.Read();
		Panel hardwarePanel = CreatePanel(30, 150, 840, 250);
		//Label hardwareTitle = CreateSectionTitle("SYSTEM HARDWARE", 20, 16);

		Panel boardCardInfo = CreateHardwareInfoCard(SingularityIconType.Motherboard, "BOARD", hardware.Mainboard, 20, 55, 800);
		Panel cpuCardInfo = CreateHardwareInfoCard(SingularityIconType.Cpu, "CPU", hardware.Cpu, 20, 115, 800);
		Panel gpuCardInfo = CreateHardwareInfoCard(SingularityIconType.Gpu, "GPU", hardware.Gpu, 20, 175, 800);

		AddSectionHeader(hardwarePanel, SingularityIconType.Motherboard, "SYSTEM HARDWARE");
		hardwarePanel.Controls.AddRange([
			boardCardInfo,
			cpuCardInfo,
			gpuCardInfo
		]);

		//Workloads Panel
		Panel workloadsPanel = CreatePanel(30, 420, 405, 365);
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
		Panel metricsPanel = CreatePanel(465, 420, 405, 365);
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
		Panel controlPanel = CreatePanel(30, 805, 840, 80);

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
			statusBadge,
			hardwarePanel,
			workloadsPanel,
			metricsPanel,
			controlPanel
		]);

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

	private static Panel CreateHardwareInfoCard(SingularityIconType iconType, string title, string value, int left, int top, int width)
	{
		Panel card = CreateCard(left, top, width, 50);
		SingularityIcon icon = new()
		{
			IconType = iconType,
			IconColor = Theme.Accent,
			Left = 12,
			Top = 9,
			Width = 32,
			Height = 32,
			BackColor = Theme.PanelLight
		};
		Label titleLabel = new()
		{
			Text = title,
			Left = 60,
			Top = 8,
			Width = 80,
			Height = 20,
			Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.PanelLight
		};
		Label valueLabel = new()
		{
			Text = value,
			Left = 60,
			Top = 25,
			Width = width - 80,
			Height = 20,
			Font = new Font("Consolas", 8.5F, FontStyle.Regular),
			ForeColor = Theme.TextMain,
			BackColor = Theme.PanelLight,
			AutoEllipsis = true
		};

		card.Controls.AddRange([
			icon,
			titleLabel,
			valueLabel
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