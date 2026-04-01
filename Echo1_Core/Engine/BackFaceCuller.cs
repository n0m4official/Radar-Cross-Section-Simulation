// Echo1_RcsSimulator\Echo1_Core\Engine\BackFaceCuller.cs
using Echo1.Core.Geometry;
using System.Numerics;

namespace Echo1.Core.Engine;

public static class BackFaceCuller
{
	/// <summary>Returns only facets whose normal points toward the radar.</summary>
	public static IEnumerable<Facet> Cull(IEnumerable<Facet> facets, Vector3 radarDir)
	{
		return facets.Where(f =>
		{
			// 1. Check if the normal is 'broken' (zero-length or NaN)
			// A valid normalized vector should have a LengthSquared very close to 1.0
			float lensq = f.Normal.LengthSquared();
			if (lensq < 1e-6f || float.IsNaN(lensq))
				return false;

			// 2. Original back-face check
			return Vector3.Dot(f.Normal, radarDir) > 0f;
		});
	}
}