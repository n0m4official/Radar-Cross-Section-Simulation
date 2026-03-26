// Echo1_RcsSimulator\Echo1_Core\Geometry\MeshDecimator.cs
using Echo1.Core.Geometry;

namespace Echo1.Core.Geometry;

public static class MeshDecimator
{
	/// <summary>
	/// Simple uniform decimation — keeps every Nth facet.
	/// Replace with quadric error metrics (QEM) in Phase 3.
	/// </summary>
	public static RcsMesh Decimate(RcsMesh source, int targetFacetCount)
	{
		if (targetFacetCount >= source.Facets.Length) return source;

		int step = source.Facets.Length / Math.Max(1, targetFacetCount);
		var kept = source.Facets
			.Where((_, i) => i % step == 0)
			.Select((f, i) => new Facet(i, f.V0, f.V1, f.V2))
			.ToArray();

		return new RcsMesh(source.Name + $"_lod{targetFacetCount}", kept);
	}
}