using Echo1.Core.Engine;
using Echo1.Core.Geometry;
using System.Numerics;

namespace Echo1.Core.Geometry;

public sealed class RcsMesh
{
	public string Name { get; }
	public Facet[] Facets { get; }
	public BoundingBox Bounds { get; }

	/// <summary>
	/// Edge record now stores both face normals for correct UTD angle computation.
	/// </summary>
	public readonly record struct Edge(
		int Index,
		Vector3 A,
		Vector3 B,
		float WedgeAngle,
		Vector3 Normal1,   // outward normal of face 1
		Vector3 Normal2);  // outward normal of face 2

	public Edge[] Edges { get; private set; }

	public RcsMesh[] LodLevels { get; private set; } = Array.Empty<RcsMesh>();

	// Per-facet material assignment — null means use MeshDefaultMaterial.
	private MaterialProperties?[] _facetMaterials;

	/// <summary>Default material for facets without explicit assignment (defaults to PEC).</summary>
	public MaterialProperties MeshDefaultMaterial { get; set; } = MaterialProperties.PEC;

	public RcsMesh(string name, Facet[] facets)
	{
		Name = name;
		Facets = facets;
		Bounds = BoundingBox.FromFacets(facets);
		Edges = Array.Empty<Edge>();
		_facetMaterials = new MaterialProperties?[facets.Length];
	}

	public MaterialProperties GetMaterial(int facetIndex)
		=> _facetMaterials[facetIndex] ?? MeshDefaultMaterial;

	public void SetMaterial(int facetIndex, MaterialProperties material)
	{
		if (facetIndex >= 0 && facetIndex < _facetMaterials.Length)
			_facetMaterials[facetIndex] = material;
	}

	/// <summary>
	/// Assign a material to all facets whose centroid falls within the given AABB.
	/// Useful for tagging specific surface zones (e.g. RAM panels on wing leading edge).
	/// </summary>
	public int SetMaterialByRegion(MaterialProperties material,
		Vector3 regionMin, Vector3 regionMax)
	{
		int count = 0;
		for (int i = 0; i < Facets.Length; i++)
		{
			var c = Facets[i].Centroid;
			if (c.X >= regionMin.X && c.X <= regionMax.X &&
				c.Y >= regionMin.Y && c.Y <= regionMax.Y &&
				c.Z >= regionMin.Z && c.Z <= regionMax.Z)
			{
				_facetMaterials[i] = material;
				count++;
			}
		}
		return count;
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
	/// Extract unique edges and estimate wedge angles + face normals for diffraction.
	/// Uses exact dihedral angle computation from shared face normals.
	/// Call once after loading.
	/// </summary>
	public void BuildEdges()
	{
		// Maps canonical vertex-pair key → (A, B, N1) for the first face sharing this edge
		var edgeMap = new Dictionary<long, (Vector3 A, Vector3 B, Vector3 N1)>();
		var result = new List<Edge>();

		for (int fi = 0; fi < Facets.Length; fi++)
		{
			var f = Facets[fi];
			var verts = new[] { f.V0, f.V1, f.V2 };

			for (int ei = 0; ei < 3; ei++)
			{
				var a = verts[ei];
				var b = verts[(ei + 1) % 3];
				long key = MakeEdgeKey(a, b);

				if (edgeMap.TryGetValue(key, out var existing))
				{
					// Shared edge — compute exact dihedral (interior wedge) angle
					float cosAngle = Vector3.Dot(existing.N1, f.Normal);
					cosAngle = Math.Clamp(cosAngle, -1f, 1f);
					// Interior solid angle of the wedge = π - dihedral (exterior angle)
					float dihedralAngle = MathF.Acos(cosAngle);
					float wedgeInteriorAngle = MathF.PI - dihedralAngle;

					result.Add(new Edge(result.Count, a, b,
						wedgeInteriorAngle,
						existing.N1,
						f.Normal));

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

	/// <summary>
	/// Deterministic canonical key for an edge from two vertex positions.
	/// Encodes both vertices as a 64-bit key regardless of winding order.
	/// </summary>
	private static long MakeEdgeKey(Vector3 a, Vector3 b)
	{
		// Quantise to 1mm grid to merge near-coincident vertices
		long ha = HashVertex(a);
		long hb = HashVertex(b);
		return ha < hb ? (ha * 397L) ^ hb : (hb * 397L) ^ ha;
	}

	private static long HashVertex(Vector3 v)
	{
		// Round to nearest millimetre
		long ix = (long)Math.Round(v.X * 1000.0);
		long iy = (long)Math.Round(v.Y * 1000.0);
		long iz = (long)Math.Round(v.Z * 1000.0);
		return ix * 1_000_003L + iy * 1_009L + iz;
	}
}
