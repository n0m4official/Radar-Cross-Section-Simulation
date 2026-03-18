namespace Echo1.Core.Radar;

public sealed class RadarConfig
{
	// Frequency
	public double FrequencyHz { get; set; } = 10e9;   // X-band default
	public double WavelengthM => PhysicsConstants.C / FrequencyHz;
	public double WaveNumber => 2.0 * Math.PI / WavelengthM;

	// Geometry
	public float AzimuthDeg { get; set; }
	public float ElevationDeg { get; set; }
	public float SweepRateDegPerSec { get; set; } = 30f;

	// Polarisation (future)
	public Polarisation TxPol { get; set; } = Polarisation.VV;

	public System.Numerics.Vector3 IncidentDirection =>
		AngleToVector(AzimuthDeg, ElevationDeg);

	public static System.Numerics.Vector3 AngleToVector(float az, float el)
	{
		float azR = MathF.PI * az / 180f;
		float elR = MathF.PI * el / 180f;
		return new(
			MathF.Cos(elR) * MathF.Sin(azR),
			MathF.Sin(elR),
			MathF.Cos(elR) * MathF.Cos(azR)
		);
	}
}

public enum Polarisation { HH, VV, HV, VH }

public static class PhysicsConstants
{
	public const double C = 299_792_458.0; // m/s
}