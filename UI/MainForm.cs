using Singularity.Core;
using Singularity.Monitoring;

namespace Singularity.UI;

/// <summary>
/// Hauptfenster der Anwendung.
/// Die UI nimmt Eingaben entgegen, zeigt Messwerte an
/// und ruft den StressController auf.
/// Die eigentliche Lastlogik liegt bewusst nicht in dieser Klasse.
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

	private readonly Button startButton = new();
	private readonly Button stopButton = new();

	private readonly Label titleLabel = new();
	private readonly Label statusLabel = new();
	private readonly Label cpuLabel = new();
	private readonly Label appMemoryLabel = new();
	private readonly Label systemMemoryLabel = new();
	private readonly Label gpuLabel = new();

	private readonly System.Windows.Forms.Timer timer = new();

	public MainForm()
	{
		Text = "Singularity";
		Width = 620;
		Height = 450;
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;

		BuildUi();

		timer.Interval = 1000;
		timer.Tick += (_, _) => UpdateMonitoring();
		timer.Start();
	}

	private void BuildUi()
	{
		titleLabel.Text = "SINGULARITY";
		titleLabel.Left = 20;
		titleLabel.Top = 20;
		titleLabel.Width = 560;
		titleLabel.Height = 50;
		titleLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold);

		cpuCheck.Text = "CPU Workload";
		cpuCheck.Left = 20;
		cpuCheck.Top = 80;
		cpuCheck.Width = 160;
		cpuCheck.Checked = true;

		Label cpuThreadsLabel = new()
		{
			Text = "CPU Threads",
			Left = 240,
			Top = 82,
			Width = 120
		};

		cpuThreadsInput.Left = 380;
		cpuThreadsInput.Top = 78;
		cpuThreadsInput.Width = 100;
		cpuThreadsInput.Minimum = 1;
		cpuThreadsInput.Maximum = Environment.ProcessorCount * 4;
		cpuThreadsInput.Value = Environment.ProcessorCount;

		memoryCheck.Text = "RAM Workload";
		memoryCheck.Left = 20;
		memoryCheck.Top = 120;
		memoryCheck.Width = 160;
		memoryCheck.Checked = true;

		Label memoryGbLabel = new()
		{
			Text = "RAM GB",
			Left = 240,
			Top = 122,
			Width = 120
		};

		memoryGbInput.Left = 380;
		memoryGbInput.Top = 118;
		memoryGbInput.Width = 100;
		memoryGbInput.Minimum = 1;
		memoryGbInput.Maximum = 1024;
		memoryGbInput.Value = 8;

		gpuCheck.Text = "GPU Workload vorbereiten";
		gpuCheck.Left = 20;
		gpuCheck.Top = 160;
		gpuCheck.Width = 230;
		gpuCheck.Enabled = true;

		startButton.Text = "START";
		startButton.Left = 20;
		startButton.Top = 215;
		startButton.Width = 220;
		startButton.Height = 45;
		startButton.Click += (_, _) => StartWorkload();

		stopButton.Text = "STOP";
		stopButton.Left = 260;
		stopButton.Top = 215;
		stopButton.Width = 220;
		stopButton.Height = 45;
		stopButton.Enabled = false;
		stopButton.Click += (_, _) => StopWorkload();

		statusLabel.Text = "Status: Bereit";
		statusLabel.Left = 20;
		statusLabel.Top = 285;
		statusLabel.Width = 560;

		cpuLabel.Left = 20;
		cpuLabel.Top = 315;
		cpuLabel.Width = 560;

		appMemoryLabel.Left = 20;
		appMemoryLabel.Top = 340;
		appMemoryLabel.Width = 560;

		systemMemoryLabel.Left = 20;
		systemMemoryLabel.Top = 365;
		systemMemoryLabel.Width = 560;

		gpuLabel.Left = 20;
		gpuLabel.Top = 390;
		gpuLabel.Width = 560;
		gpuLabel.Text = "GPU: vorbereitet, echte Direct3D-Last folgt später";

		Controls.AddRange([
			titleLabel,
			cpuCheck,
			cpuThreadsLabel,
			cpuThreadsInput,
			memoryCheck,
			memoryGbLabel,
			memoryGbInput,
			gpuCheck,
			startButton,
			stopButton,
			statusLabel,
			cpuLabel,
			appMemoryLabel,
			systemMemoryLabel,
			gpuLabel
		]);
	}

	private void StartWorkload()
	{
		WorkloadOptions options = new()
		{
			EnableCpuWorkload = cpuCheck.Checked,
			EnableMemoryWorkload = memoryCheck.Checked,
			EnableGpuWorkload = gpuCheck.Checked,
			CpuThreads = (int)cpuThreadsInput.Value,
			MemoryGb = (int)memoryGbInput.Value
		};

		workloadController.Configure(options);
		workloadController.Start();

		SetInputEnabled(false);

		startButton.Enabled = false;
		stopButton.Enabled = true;

		statusLabel.Text = "Status: Singularity läuft";
	}

	private void StopWorkload()
	{
		workloadController.Stop();

		SetInputEnabled(true);

		startButton.Enabled = true;
		stopButton.Enabled = false;

		statusLabel.Text = "Status: Gestoppt";
	}

	private void SetInputEnabled(bool enabled)
	{
		cpuCheck.Enabled = enabled;
		memoryCheck.Enabled = enabled;
		gpuCheck.Enabled = enabled;
		cpuThreadsInput.Enabled = enabled;
		memoryGbInput.Enabled = enabled;
	}

	private void UpdateMonitoring()
	{
		SystemSnapshot snapshot = systemMonitor.GetSnapshot();

		cpuLabel.Text = $"App CPU Load: {snapshot.ProcessCpuPercent:F1}%";
		appMemoryLabel.Text = $"App RAM: {snapshot.ProcessMemoryMb:N0} MB";
		systemMemoryLabel.Text =
			$"System RAM: {snapshot.UsedPhysicalMemoryMb:N0} MB / {snapshot.TotalPhysicalMemoryMb:N0} MB ({snapshot.UsedPhysicalMemoryPercent:F1}%)";
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		workloadController.Stop();
		base.OnFormClosing(e);
	}
}