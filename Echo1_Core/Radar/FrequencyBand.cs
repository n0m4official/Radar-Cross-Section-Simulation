namespace Echo1_Core.Radar
{
	public record FrequencyBand(string Name, double MinFreqHz, double MaxFreqHz)
	{
		public static readonly FrequencyBand XBand = new("X-Band", 8e9, 12e12);
		public static readonly FrequencyBand KaBand = new("Ka-Band", 26.5e9, 40e9);
		public static readonly FrequencyBand LBand = new("L-Band", 1e9, 2e9);

		public double CenterFreq => (MinFreqHz + MaxFreqHz) / 2.0;
	}
}