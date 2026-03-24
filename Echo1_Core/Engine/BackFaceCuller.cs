using Echo1_Core.Geometry;
using System.Numerics;

namespace Echo1_Core.Engine
{
	internal static class BackFaceCuller
	{
		// Determines if a facet is visible to the radar source
		public static bool IsVisible(Facet facet, Vector3 incidentDir)
		{
			// Incident direction is 'toward' the target, so we negate it for the dot product
			return Vector3.Dot(facet.Normal, -incidentDir) > 0;
        }
	}
}