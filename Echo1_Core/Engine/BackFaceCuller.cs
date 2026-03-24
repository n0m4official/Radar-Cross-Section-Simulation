using Echo1.Core.Geometry;
using System.Numerics;

namespace Echo1.Core.Engine;

public static class BackFaceCuller
{
	/// <summary>Returns only facets whose normal points toward the radar.</summary>
	public static IEnumerable<Facet> Cull(IEnumerable<Facet> facets, Vector3 radarDir)
		=> facets.Where(f => Vector3.Dot(f.Normal, radarDir) > 0f);
}