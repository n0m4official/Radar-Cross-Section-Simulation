// Echo1_RcsSimulator\Echo1_Core\Engine\RcsCache.cs
using Echo1.Core.Radar;
using System.Runtime.Caching;

namespace Echo1.Core.Engine;

public sealed class RcsCache
{
	private readonly MemoryCache _store = new("RcsCache");
	private readonly CacheItemPolicy _policy = new()
	{
		SlidingExpiration = TimeSpan.FromSeconds(120)
	};

	public bool TryGet(CacheKey key, out RcsResult result)
	{
		result = _store[key.ToString()] as RcsResult ?? null!;
		return result != null;
	}

	public void Store(CacheKey key, RcsResult result)
		=> _store.Set(key.ToString(), result, _policy);

	/// <summary>
	/// Fully invalidates the cache — required when material or polarisation changes,
	/// since those affect all cached entries.
	/// </summary>
	public void Invalidate() => _store.Trim(100);
}

public readonly record struct CacheKey(int AzBin, int ElBin, int FreqBin, int PolBin)
{
	// 0.5° angular resolution, 100 MHz frequency resolution
	public static CacheKey From(RadarConfig r)
		=> new(
			(int)(r.AzimuthDeg / 0.5f),
			(int)(r.ElevationDeg / 0.5f),
			(int)(r.FrequencyHz / 1e8),
			(int)r.TxPol);

	public override string ToString() => $"{AzBin}:{ElBin}:{FreqBin}:{PolBin}";
}

// Extension on RcsEngine to expose cache invalidation
public sealed partial class RcsEngine
{
	public void InvalidateCache() => _cache.Invalidate();
}
