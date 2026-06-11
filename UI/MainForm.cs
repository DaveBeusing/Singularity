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
	private readonly CheckBox cpuCheck = new();
	private readonly CheckBox memoryCheck = new();
	private readonly CheckBox gpuCheck = new();

	private readonly NumericUpDown cpuThreadsInput = new();
	private readonly NumericUpDown memoryGbInput = new();
	private readonly NumericUpDown gpuLoadInput = new();
	private readonly Button startButton = new();
	private readonly Button stopButton = new();
	private readonly Label statusBadge = new();
	private readonly Label cpuMetricValue = new();
	private readonly Label appRamMetricValue = new();
	private readonly Label systemRamMetricValue = new();
	private readonly System.Windows.Forms.Timer timer = new();

	public MainForm()
	{
		Text = "Singularity";
		ClientSize = new Size(900, 600);
		MinimumSize = new Size(900, 600);
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
			Text = "SINGULARITY",
			Left = 30,
			Top = 30,
			Width = 420,
			Height = 42,
			Font = new Font("Segoe UI", 24F, FontStyle.Bold),
			ForeColor = Theme.TextMain,
			BackColor = Theme.Background
		};

		Label subtitle = new()
		{
			Text = "Lightweight Stresstest and Burn-In Tool",
			Left = 32,
			Top = 76,
			Width = 520,
			Height = 26,
			Font = new Font("Segoe UI", 10F),
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Background
		};

		statusBadge.Left = 680;
		statusBadge.Top = 32;
		statusBadge.Width = 130;
		statusBadge.Height = 32;
		statusBadge.Text = "READY";
		statusBadge.TextAlign = ContentAlignment.MiddleCenter;
		statusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
		statusBadge.BackColor = Theme.PanelLight;
		statusBadge.ForeColor = Theme.TextMain;

		//Workloads Section
		Panel workloadsPanel = CreatePanel(30, 135, 390, 330);
		SingularityIcon workloadsIcon = new()
		{
			IconType = SingularityIconType.Cpu,
			IconColor = Theme.Accent,
			Left = 20,
			Top = 14,
			Width = 32,
			Height = 32,
			BackColor = Theme.Panel
		};
		Label workloadsTitle = CreateSectionTitle("WORKLOADS", 60, 16);

		//CPU Workload
		Panel cpuCard = CreateCard(20, 60, 350, 72);
		ConfigureCheckBox(cpuCheck, "CPU", 80, 20, true);
		SingularityIcon cpuIcon = CreateIcon(SingularityIconType.Cpu, 20, 20, Theme.TextMuted);
		Label cpuThreadsLabel = CreateMutedLabel("Threads", 160, 22, 75);
		ConfigureNumeric(cpuThreadsInput, 250, 20, Environment.ProcessorCount, 1, Environment.ProcessorCount * 4);
		cpuCard.Controls.Add(cpuCheck);
		cpuCard.Controls.Add(cpuIcon);
		cpuCard.Controls.Add(cpuThreadsLabel);
		cpuCard.Controls.Add(cpuThreadsInput);

		//RAM Workload
		Panel ramCard = CreateCard(20, 145, 350, 72);
		ConfigureCheckBox(memoryCheck, "RAM", 80, 20, true);
		SingularityIcon memoryIcon = CreateIcon(SingularityIconType.Memory, 20, 20, Theme.TextMuted);
		Label ramGbLabel = CreateMutedLabel("GB", 160, 22, 40);
		ConfigureNumeric(memoryGbInput, 250, 20, 8, 1, 1024);
		ramCard.Controls.Add(memoryCheck);
		ramCard.Controls.Add(memoryIcon);
		ramCard.Controls.Add(ramGbLabel);
		ramCard.Controls.Add(memoryGbInput);

		//GPU Workload
		Panel gpuCard = CreateCard(20, 230, 350, 72);
		ConfigureCheckBox(gpuCheck, "GPU", 80, 20, false);
		SingularityIcon gpuIcon = CreateIcon(SingularityIconType.Gpu, 20, 20, Theme.TextMuted);
		Label gpuLoadLabel = CreateMutedLabel("Load %", 160, 22, 75);
		ConfigureNumeric( gpuLoadInput, 250, 20, 100, 1, 100);
		gpuCard.Controls.Add(gpuCheck);
		gpuCard.Controls.Add(gpuIcon);
		gpuCard.Controls.Add(gpuLoadLabel);
		gpuCard.Controls.Add(gpuLoadInput);


		workloadsPanel.Controls.Add(workloadsIcon);
		workloadsPanel.Controls.Add(workloadsTitle);
		workloadsPanel.Controls.Add(cpuCard);
		workloadsPanel.Controls.Add(ramCard);
		workloadsPanel.Controls.Add(gpuCard);


		//Metrics Section
		Panel metricsPanel = CreatePanel(450, 135, 390, 330);
		SingularityIcon metricsIcon = new()
		{
			IconType = SingularityIconType.Metrics,
			IconColor = Theme.Accent,
			Left = 20,
			Top = 14,
			Width = 32,
			Height = 32,
			BackColor = Theme.Panel
		};
		Label metricsTitle = CreateSectionTitle("LIVE METRICS", 60, 16);
		Panel cpuMetric = CreateMetricCard("APP CPU", cpuMetricValue, 20, 60);
		Panel appRamMetric = CreateMetricCard("APP RAM", appRamMetricValue, 20, 145);
		Panel systemRamMetric = CreateMetricCard("SYSTEM RAM", systemRamMetricValue, 20, 230);
		metricsPanel.Controls.Add(metricsIcon);
		metricsPanel.Controls.Add(metricsTitle);
		metricsPanel.Controls.Add(cpuMetric);
		metricsPanel.Controls.Add(appRamMetric);
		metricsPanel.Controls.Add(systemRamMetric);

		//Control Section
		Panel controlPanel = CreatePanel(30, 495, 810, 75);

		startButton.Text = "START TEST";
		startButton.Left = 20;
		startButton.Top = 17;
		startButton.Width = 230;
		startButton.Height = 42;
		StyleButton(startButton, Theme.Accent, Color.Black);
		startButton.Click += (_, _) => StartWorkload();

		stopButton.Text = "STOP TEST";
		stopButton.Left = 270;
		stopButton.Top = 17;
		stopButton.Width = 230;
		stopButton.Height = 42;
		StyleButton(stopButton, Theme.Danger, Color.White);
		stopButton.Enabled = false;
		stopButton.Click += (_, _) => StopWorkload();

		Label hint = new()
		{
			Text = "Tip: Start with moderate RAM values before running full burn-in.",
			Left = 530,
			Top = 25,
			Width = 260,
			Height = 28,
			ForeColor = Theme.TextMuted,
			BackColor = Theme.Panel
		};

		controlPanel.Controls.Add(startButton);
		controlPanel.Controls.Add(stopButton);
		controlPanel.Controls.Add(hint);

		//Controls.Add(title);
		Controls.Add(subtitle);
		Controls.Add(statusBadge);
		Controls.Add(workloadsPanel);
		Controls.Add(metricsPanel);
		Controls.Add(controlPanel);
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

	private static void ConfigureCheckBox(
		CheckBox checkBox,
		string text,
		int left,
		int top,
		bool isChecked)
	{
		checkBox.Text = text;
		checkBox.Left = left;
		checkBox.Top = top;
		checkBox.Width = 80;
		checkBox.Height = 30;
		checkBox.Checked = isChecked;
		checkBox.ForeColor = Theme.TextMain;
		checkBox.BackColor = Theme.PanelLight;
		checkBox.FlatStyle = FlatStyle.Flat;
	}

	private static void ConfigureNumeric(
		NumericUpDown numeric,
		int left,
		int top,
		int value,
		int minimum,
		int maximum)
	{
		numeric.Left = left;
		numeric.Top = top;
		numeric.Width = 75;
		numeric.Height = 28;
		numeric.Minimum = minimum;
		numeric.Maximum = maximum;
		numeric.Value = value;
		numeric.BackColor = Theme.Background;
		numeric.ForeColor = Theme.TextMain;
		numeric.BorderStyle = BorderStyle.FixedSingle;
		numeric.Font = new Font("Segoe UI", 9.5F);
	}

	private static Panel CreateMetricCard(string title, Label valueLabel, int left, int top)
	{
		Panel card = CreateCard(left, top, 350, 72);

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
		valueLabel.Top = 34;
		valueLabel.Width = 315;
		valueLabel.Height = 30;
		valueLabel.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
		valueLabel.ForeColor = Theme.TextMain;
		valueLabel.BackColor = Theme.PanelLight;
		valueLabel.Text = "-";
		valueLabel.AutoEllipsis = true;

		card.Controls.Add(titleLabel);
		card.Controls.Add(valueLabel);

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
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		workloadController.Stop();
		base.OnFormClosing(e);
	}
}