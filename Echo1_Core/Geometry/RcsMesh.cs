// Echo1_RcsSimulator\Echo1_Core\Geometry\RcsMesh.cs
using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using System.Numerics;

namespace Echo1.Core.Geometry;

public sealed class RcsMesh
{
	public string Name { get; }
	public Facet[] Facets { get; }
	public BoundingBox Bounds { get; }

	public record struct Edge(int Index, Vector3 A, Vector3 B, float WedgeAngle);
	public Edge[] Edges { get; private set; }

	public RcsMesh[] LodLevels { get; private set; } = Array.Empty<RcsMesh>();

	public RcsMesh(string name, Facet[] facets)
	{
		Name = name;
		Facets = facets;
		Bounds = BoundingBox.FromFacets(facets);
		Edges = Array.Empty<Edge>();   // populated by BuildEdges()
	}

	public RcsMesh SelectLod(float distanceMetres, float fovDegrees)
	{
		if (LodLevels.Length == 0) return this;
		float coverage = Bounds.DiagonalMetres / distanceMetres;
		return coverage switch
		{
			> 0.5f => this,
			> 0.1f => LodLevels[0],
			_ => LodLevels[^1]
		};
	}

	public void BuildLods(int[] targetFacetCounts)
	{
		LodLevels = targetFacetCounts
			.Select(t => MeshDecimator.Decimate(this, t))
			.ToArray();
	}

	/// <summary>
	/// Extract unique edges and estimate wedge angles for diffraction.
	/// Call once after loading.
	/// </summary>
	public void BuildEdges()
	{
		var edgeMap = new Dictionary<(int, int), (Vector3 A, Vector3 B, Vector3 N1)>();
		var result = new List<Edge>();

		for (int fi = 0; fi < Facets.Length; fi++)
		{
			var f = Facets[fi];
			var verts = new[] { f.V0, f.V1, f.V2 };
			for (int ei = 0; ei < 3; ei++)
			{
				var a = verts[ei];
				var b = verts[(ei + 1) % 3];
				// Canonical key — lower index first
				var key = GetHashCode(a) < GetHashCode(b)
					? (GetHashCode(a), GetHashCode(b))
					: (GetHashCode(b), GetHashCode(a));

				if (edgeMap.TryGetValue(key, out var existing))
				{
					// Shared edge — compute dihedral angle
					float cos = Vector3.Dot(existing.N1, f.Normal);
					float angle = MathF.Acos(Math.Clamp(cos, -1f, 1f));
					result.Add(new Edge(result.Count, a, b, angle));
					edgeMap.Remove(key);
				}
				else
				{
					edgeMap[key] = (a, b, f.Normal);
				}
			}
		}
		Edges = result.ToArray();
	}

	private static int GetHashCode(Vector3 v)
		=> HashCode.Combine(v.X, v.Y, v.Z);
}