using Echo1.Core.Geometry;

namespace Echo1.Core.Multiphysics;

public readonly record struct ThermalLink(
	int FacetA,
	int FacetB,
	double SharedEdgeLengthM,
	double CentroidDistanceM);

public sealed class ThermalMeshTopology
{
	public ThermalLink[] Links { get; }

	private ThermalMeshTopology(ThermalLink[] links) => Links = links;

	public static ThermalMeshTopology From(RcsMesh mesh)
	{
		var links = new List<ThermalLink>(mesh.Edges.Length);

		foreach (var edge in mesh.Edges)
		{
			double edgeLength = (edge.B - edge.A).Length();
			double centroidDistance = (
				mesh.Facets[edge.Facet1Index].Centroid
				- mesh.Facets[edge.Facet2Index].Centroid).Length();

			if (edgeLength <= 1e-9 || centroidDistance <= 1e-9)
				continue;

			links.Add(new ThermalLink(
				edge.Facet1Index,
				edge.Facet2Index,
				edgeLength,
				centroidDistance));
		}

		return new ThermalMeshTopology(links.ToArray());
	}
}