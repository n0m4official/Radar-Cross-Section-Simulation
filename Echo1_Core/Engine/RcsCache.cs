using Echo1.Core.Radar;
using System.Runtime.Caching;

namespace Echo1.Core.Engine;

public sealed class RcsCache
{
	private readonly MemoryCache _store = new("RcsCache");
	private readonly CacheItemPolicy _policy = new()
	{
		SlidingExpiration = TimeSpan.FromSeconds(60)
	};

	// Angular quantisation: 0.5° resolution
	private const float AngleRes = 0.5f;

	public bool TryGet(CacheKey key, out RcsResult result)
	{
		result = _store[key.ToString()] as RcsResult ?? null!;
		return result != null;
	}

	public void Store(CacheKey key, RcsResult result)
		=> _store.Set(key.ToString(), result, _policy);

	public void Invalidate() => _store.Trim(100);
}

public readonly record struct CacheKey(
	int AzBin, int ElBin, int FreqBin)
{
	public static CacheKey From(RadarConfig r)
		=> new(
			(int)(r.AzimuthDeg / 0.5f),
			(int)(r.ElevationDeg / 0.5f),
			(int)(r.FrequencyHz / 1e8));   // 100 MHz bins

	public override string ToString() => $"{AzBin}:{ElBin}:{FreqBin}";
}