namespace Singularity.Monitoring;

public sealed class HardwareInfo
{
	public string Mainboard { get; set; } = "Unknown";
	public string Cpu { get; set; } = "Unknown";
	public string Gpu { get; set; } = "Unknown";
}