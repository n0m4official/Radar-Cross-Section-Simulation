using System.Numerics;
using Echo1.Core.Geometry;
using Echo1.Core.Radar;

namespace Echo1.Core.Multiphysics;

public static class ElectromagneticAbsorptionKernel
{
	public static double[] ComputeAbsorbedPower(
		RcsMesh mesh,
		RadarConfig radar,
		FacetState state)
	{
		var absorbedPower = new double[mesh.Facets.Length];
		Vector3 incidentDirection = radar.IncidentDirection;

		for (int i = 0; i < mesh.Facets.Length; i++)
		{
			Facet facet = mesh.Facets[i];

			double cosIncidence = Math.Max(
				0.0,
				Vector3.Dot(facet.Normal, -incidentDirection));

			if (cosIncidence <= 0.0)
				continue;

			var material = mesh.GetMaterial(i);

			var gamma = material.FresnelReflection(
				cosIncidence,
				radar.TxPol,
				radar.FrequencyHz,
				state.TemperatureK[i]);

			double reflectance = Math.Clamp(
				gamma.Real * gamma.Real + gamma.Imaginary * gamma.Imaginary,
				0.0,
				1.0);

			// Assumes an opaque, backed material: transmissivity = 0.
			double absorptance = 1.0 - reflectance;

			absorbedPower[i] =
				radar.IncidentPowerFluxWm2
				* facet.Area
				* cosIncidence
				* absorptance;
		}

		return absorbedPower;
	}
}