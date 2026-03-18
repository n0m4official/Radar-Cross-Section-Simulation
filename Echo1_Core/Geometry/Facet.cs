using System.Numerics;

namespace Echo1_Core.Geometry;

/// <summary>
/// Represents one triangular facet of the target mesh
/// All geometry pre-computed once at import
/// </summary>

public sealed class Facet
{
	public readonly int Index;
	public readonly Vector3 V0, V1, V2;
	public readonly Vector3 Centroid;
	public readonly Vector3 Normal;		// outward unit normal
	public readonly float Area;         // m^2

	// Updated each frame - written by RcsEngine, read by renderer
	public volatile float RcsContribution;	// m^2 (linear)
	public volatile float RcsDb;			// dBsm

	public Facet(int index, Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Index = index;
		V0 = v0;
		V1 = v1;
		V2 = v2;
		Centroid = (v0 + v1 + v2) / 3f;

		var edge1	= v1 - v0;
		var edge2	= v2 - v0;
		var cross	= Vector3.Cross(edge1, edge2);
		Area		= cross.Length() / 2f;
		Normal = Area > 1e-10f ? Vector3.Normalize(cross) : Vector3.UnitZ;
	}

	[System.Runtime.CompilerServices.MethodImpl(
		System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public bool FacesRadar(Vector3 radarDir)
		=> Vector3.Dot(Normal, radarDir) > 0f;
}
