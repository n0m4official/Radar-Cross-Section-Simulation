namespace Echo1_Core.Radar;

public class RadarSweepState
{
	public bool IsEnabled { get; set; }
	public float StartAzimuth { get; set; } = -180f;
	public float EndAzimuth { get; set; } = 180f;
	public float CurrentAzimuth { get; set; }
	public float StepSize { get; set; } = 0.5f;
}