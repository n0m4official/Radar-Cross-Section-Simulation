using Echo1.Core.Geometry;

namespace Echo1.Core.Multiphysics;

public sealed class FacetThermalSolver
{
	private const double StefanBoltzmann = 5.670374419e-8;

	public ThermalStepResult Step(
		RcsMesh mesh,
		ThermalMeshTopology topology,
		FacetState state,
		FlowConditions flow,
		ExternalFlowModel flowModel,
		double dtSeconds)
	{
		int count = mesh.Facets.Length;
		var previousTemperature = (double[])state.TemperatureK.Clone();
		var netPowerW = (double[])state.AbsorbedPowerW.Clone();

		double h = flowModel.GetConvectionCoefficient(flow);
		double heatIntoAirW = 0.0;

		for (int i = 0; i < count; i++)
		{
			var facet = mesh.Facets[i];
			var thermal = mesh.GetMaterial(i).Thermal;
			double area = facet.Area;

			state.ConvectionCoefficientWm2K[i] = h;

			double convectionW =
				h * area * (previousTemperature[i] - flow.AirTemperatureK);

			double radiationW =
				thermal.Emissivity
				* StefanBoltzmann
				* area
				* (Math.Pow(previousTemperature[i], 4)
					- Math.Pow(flow.AmbientTemperatureK, 4));

			netPowerW[i] -= convectionW + radiationW;
			heatIntoAirW += convectionW;
		}

		foreach (var link in topology.Links)
		{
			var materialA = mesh.GetMaterial(link.FacetA).Thermal;
			var materialB = mesh.GetMaterial(link.FacetB).Thermal;

			double conductivity = 2.0 * materialA.ConductivityWmK
				* materialB.ConductivityWmK
				/ (materialA.ConductivityWmK + materialB.ConductivityWmK);

			double thickness = Math.Min(
				materialA.ThicknessM,
				materialB.ThicknessM);

			double conductanceWperK =
				conductivity
				* link.SharedEdgeLengthM
				* thickness
				/ link.CentroidDistanceM;

			double transferW = conductanceWperK
				* (previousTemperature[link.FacetB]
					- previousTemperature[link.FacetA]);

			netPowerW[link.FacetA] += transferW;
			netPowerW[link.FacetB] -= transferW;
		}

		double maxDeltaK = 0.0;

		for (int i = 0; i < count; i++)
		{
			var facet = mesh.Facets[i];
			var thermal = mesh.GetMaterial(i).Thermal;

			double heatCapacityJperK =
				thermal.DensityKgM3
				* thermal.SpecificHeatJkgK
				* facet.Area
				* thermal.ThicknessM;

			heatCapacityJperK = Math.Max(heatCapacityJperK, 1e-9);

			double deltaK = netPowerW[i] * dtSeconds / heatCapacityJperK;
			state.TemperatureK[i] = Math.Max(1.0, previousTemperature[i] + deltaK);
			maxDeltaK = Math.Max(maxDeltaK, Math.Abs(deltaK));
		}

		return new ThermalStepResult(maxDeltaK, heatIntoAirW);
	}
}

public readonly record struct ThermalStepResult(
	double MaxTemperatureChangeK,
	double HeatIntoAirW);