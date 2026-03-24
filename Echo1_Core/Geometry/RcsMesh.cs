using Echo1_Core.Geometry;
using Echo1_Core.Engine;
using System.Numerics;
using System.Collections.Concurrent;

namespace Echo1.Core.Geometry;

public sealed class RcsMesh
{
	public string Name { get; }
	public Facet[] Facets { get; }
	public BoundingBox Bounds { get; }

	public record struct Edge(int Index, Vector3 A, Vector3 B, float WedgeAngle);
	public Edge[] Edges { get; private set; }

	// LOD levels — pre-decimated at load time
	public RcsMesh[] LodLevels { get; private set; } = Array.Empty<RcsMesh>();

	public RcsMesh(string name, Facet[] facets)
	{
		Name = name;
		Facets = facets;
		Bounds = BoundingBox.FromFacets(facets);
		Edges = new Edge[Facets.Length];
	}

	/// <summary>Select LOD level based on pixel-coverage heuristic.</summary>
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
}