// Echo1_RcsSimulator\Echo1_Core\Geometry\BoundingBox.cs
using System.Numerics;

namespace Echo1.Core.Geometry;

public readonly struct BoundingBox
{
	public readonly Vector3 Min;
	public readonly Vector3 Max;
	public float DiagonalMetres => (Max  - Min).Length();

	public BoundingBox(Vector3 min, Vector3 max)
	{
		Min = min;
		Max = max;
	}

	public static BoundingBox FromFacets(Facet[] facets)
	{
		if (facets.Length == 0)
		{
			return new BoundingBox(Vector3.Zero, Vector3.Zero);
		}

		var min = new Vector3(float.MaxValue);
		var max = new Vector3(float.MinValue);

		foreach (var f in facets)
		{
			foreach (var v in new[] { f.V0, f.V1, f.V2 })
			{
				min = Vector3.Min(min, v);
				max = Vector3.Max(max, v);
			}
		}
		return new BoundingBox(min, max);
	}
}
