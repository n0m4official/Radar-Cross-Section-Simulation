using System.Windows.Media;
using System.Windows.Media.Media3D;
using Echo1.Core.Geometry;
using HelixToolkit.Wpf;
using Echo1.Wpf.Rendering;

namespace Echo1.Wpf.Rendering;

public sealed class SceneBuilder
{
	private readonly FacetMaterialCache _matCache = new();

	public Model3DGroup BuildHeatmapScene(RcsMesh mesh, float minDb, float maxDb)
	{
		var group = new Model3DGroup();
		const int Buckets = 64;

		// One MeshGeometry3D per color bucket
		var positions = new Point3DCollection[Buckets];
		var indices = new Int32Collection[Buckets];
		var vertCounts = new int[Buckets];

		for (int i = 0; i < Buckets; i++)
		{
			positions[i] = new Point3DCollection();
			indices[i] = new Int32Collection();
		}

		foreach (var facet in mesh.Facets)
		{
			float t = maxDb > minDb
				? (facet.RcsDb - minDb) / (maxDb - minDb)
				: 0.5f;
			t = Math.Max(0f, Math.Min(1f, t));
			int bucket = (int)(t * (Buckets - 1));

			int baseIdx = vertCounts[bucket];
			positions[bucket].Add(facet.V0.ToPoint3D());
			positions[bucket].Add(facet.V1.ToPoint3D());
			positions[bucket].Add(facet.V2.ToPoint3D());
			indices[bucket].Add(baseIdx);
			indices[bucket].Add(baseIdx + 1);
			indices[bucket].Add(baseIdx + 2);
			vertCounts[bucket] += 3;
		}

		for (int i = 0; i < Buckets; i++)
		{
			if (positions[i].Count == 0) continue;

			var geom = new MeshGeometry3D
			{
				Positions = positions[i],
				TriangleIndices = indices[i]
			};

			float t = i / (float)(Buckets - 1);
			var color = HeatmapColorMap.Sample(t);
			var mat = _matCache.Get(color);

			group.Children.Add(new GeometryModel3D(geom, mat));
		}

		return group;
	}
}