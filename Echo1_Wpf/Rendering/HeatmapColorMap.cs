using System.Windows.Media;

namespace Echo1.Wpf.Rendering;

/// <summary>
/// Maps a normalized RCS value [0..1] to a color for facet heatmap display.
/// Uses a plasma-inspired ramp: dark blue → cyan → yellow → red.
/// </summary>
public static class HeatmapColorMap
{
	// Pre-baked 256-entry LUT — built once at startup
	private static readonly Color[] _lut = BuildLut();

	public static Color Sample(float t)
	{
		int i = Math.Clamp((int)(t * 255), 0, 255);
		return _lut[i];
	}

	public static SolidColorBrush SampleBrush(float t) => new(Sample(t));

	private static Color[] BuildLut()
	{
		var lut = new Color[256];
		for (int i = 0; i < 256; i++)
		{
			float t = i / 255f;
			// Plasma approximation
			float r = MathF.Pow(MathF.Clamp(1.9f * t - 0.5f, 0, 1), 0.8f);
			float g = MathF.Sin(MathF.PI * t) * 0.9f;
			float b = MathF.Clamp(1.4f - 2.5f * t, 0, 1);
			lut[i] = Color.FromRgb(
				(byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
		}
		return lut;
	}
}