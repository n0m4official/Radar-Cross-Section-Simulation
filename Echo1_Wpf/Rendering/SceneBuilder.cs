using Echo1_Wpf.Rendering;
using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Echo1.Wpf.Rendering;

public sealed class SceneBuilder
{
	private readonly FacetMaterialCache _matCache = new();

	/// <summary>
	/// Build a MeshGeometry3D from RcsMesh and apply per-facet heatmap color.
	/// Returns a Model3DGroup containing one GeometryModel3D per material group.
	/// Called each RCS frame on the render thread.
	/// </summary>
	public Model3DGroup BuildHeatmapScene(RcsMesh mesh, float minDb, float maxDb)
	{
		var group = new Model3DGroup();

		// Group facets by quantised color bucket (reduces geometry batches)
		const int Buckets = 64;
		var buckets = new MeshBuilder[Buckets];
		for (int i = 0; i < Buckets; i++) buckets[i] = new MeshBuilder(false, false);

		foreach (var facet in mesh.Facets)
		{
			float t = maxDb > minDb
				? (facet.RcsDb - minDb) / (maxDb - minDb)
				: 0.5f;
			t = Math.Clamp(t, 0f, 1f);
			int bucket = (int)(t * (Buckets - 1));

			var b = buckets[bucket];
			b.AddTriangle(
				facet.V0.ToPoint3D(),
				facet.V1.ToPoint3D(),
				facet.V2.ToPoint3D());
		}

		for (int i = 0; i < Buckets; i++)
		{
			if (buckets[i].Positions.Count == 0) continue;
			float t = i / (float)(Buckets - 1);
			var color = HeatmapColorMap.Sample(t);
			var mat = _matCache.Get(color);
			group.Children.Add(new GeometryModel3D(buckets[i].ToMesh(), mat));
		}

		return group;
	}
}

public static class VectorExtensions
{
	public static Point3D ToPoint3D(this System.Numerics.Vector3 v)
		=> new(v.X, v.Y, v.Z);
}